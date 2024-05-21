using Discreet.Cipher;
using Discreet.Coin.Comparers;
using Discreet.Coin.Models;
using Discreet.Common;
using Discreet.Common.Serialize;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Discreet.DB
{
    /// <summary>
    /// Provides a multiple-writer, single-reader instance for writing blocks to the blockchain. Can be configured to write periodically or on each new block.
    /// Maintains an internal state for the ValidationCache for validating block data prior to writing to the database.
    /// Wraps DataView for providing an IView.
    /// </summary>
    public class BlockBuffer : IView
    {
        private ConcurrentQueue<Block> _buffer;

        private static BlockBuffer _instance;

        public static BlockBuffer Instance
        {
            get
            {
                if (_instance == null) _instance = new BlockBuffer();
                return _instance;
            }
        }

        private static TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
        public static TimeSpan FlushInterval { set { _flushInterval = TimeSpan.FromTicks(Math.Max(value.Ticks, TimeSpan.FromSeconds(1).Ticks)); } }

        private static bool _flushEveryBlock = false;
        public static bool FlushEveryBlock { set { _flushEveryBlock = value; } }

        private List<Block> buffer = new List<Block>();
        private HashSet<Key> spentKeys = new HashSet<Key>();
        private ConcurrentDictionary<long, Block> blockCache = new ConcurrentDictionary<long, Block>();
        private ConcurrentDictionary<SHA256, long> hashesToHeights = new ConcurrentDictionary<SHA256, long>(new SHA256EqualityComparer());
        private ConcurrentDictionary<uint, TXOutput> outputCache = new ConcurrentDictionary<uint, TXOutput>();
        private ConcurrentDictionary<SHA256, FullTransaction> transactionCache = new ConcurrentDictionary<SHA256, FullTransaction>(new SHA256EqualityComparer());
        private ConcurrentDictionary<TTXInput, ScriptTXOutput> inputCache = new ConcurrentDictionary<TTXInput, ScriptTXOutput>(new TTXInputEqualityComparer());
        private uint _pIndex;
        private readonly object _pLock = new object();

        private DataView _view;

        private static readonly Block _signaler = new Block();

        /// <summary>
        /// Tries to get a block header from either the DataView or the BlockBuffer's buffer.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public bool TryGetBlockHeader(SHA256 hash, out BlockHeader header)
        {
            var err =_view.TryGetBlockHeader(hash, out var rv, false);
            if (err)
            {
                header = rv;
                return true;
            }
            else
            {
                // try getting it from the cache
                var success = hashesToHeights.TryGetValue(hash, out var height);
                if (!success)
                {
                    header = null;
                    return false;
                }

                success = blockCache.TryGetValue(height, out var blk);
                if (!success)
                {
                    header = null;
                    return false;
                }

                if (blk == null)
                {
                    header = null;
                    return false;
                }
                else
                {
                    header = blk.Header;
                    return true;
                }
            }
        }

        public uint GetOutputIndex()
        {
            lock (_pLock)
            {
                return Math.Max(_pIndex, DataView.GetView().GetOutputIndex());
            }
        }

        public BlockHeader GetBlockHeader(SHA256 hash)
        {
            try
            {
                return _view.GetBlockHeader(hash);
            }
            catch (Exception e)
            {
                var success = hashesToHeights.TryGetValue(hash, out var height);
                if (!success)
                {
                    throw;
                }

                success = blockCache.TryGetValue(height, out var blk);
                if (!success)
                {
                    throw;
                }

                if (blk == null) throw;
                return blk.Header;
            }
        }

        public BlockHeader GetBlockHeader(long height)
        {
            try
            {
                return _view.GetBlockHeader(height);
            }
            catch (Exception e)
            {
                var success = blockCache.TryGetValue(height, out var blk);
                if (!success)
                {
                    throw;
                }

                if (blk == null) throw;
                return blk.Header;
            }
        }

        public bool BlockExists(SHA256 hash)
        {
            var succ = _view.BlockExists(hash);
            
            if (succ)
            {
                return true;
            }
            else
            {
                return hashesToHeights.ContainsKey(hash);
            }
        }

        public bool ContainsTransaction(SHA256 hash)
        {
            var succ = DataView.GetView().ContainsTransaction(hash);
            if (succ)
            {
                return true;
            }
            else
            {
                return transactionCache.ContainsKey(hash);
            }
        }

        public ScriptTXOutput GetPubOutput(TTXInput tin)
        {
            try
            {
                return _view.GetPubOutput(tin);
            }
            catch (Exception e)
            {
                try
                {
                    return inputCache[tin];
                }
                catch
                {
                    throw e;
                }
            }
        }

        public bool CheckSpentKey(Key k)
        {
            lock (spentKeys)
            {
                return _view.CheckSpentKey(k) && !spentKeys.Contains(k);
            }
        }

        public TXOutput GetOutput(uint idx)
        {
            try
            {
                return DataView.GetView().GetOutput(idx);
            }
            catch (Exception e)
            {
                try
                {
                    return outputCache[idx];
                }
                catch
                {
                    throw e;
                }
            }
        }

        public TXOutput[] GetMixins(uint[] idxs)
        {
            TXOutput[] rv = new TXOutput[idxs.Length];
            for (int i = 0; i < idxs.Length; i++)
            {
                rv[i] = GetOutput(idxs[i]);
            }

            return rv;
        }

        public BlockBuffer()
        {
            _buffer = new ConcurrentQueue<Block>();
            _view = DataView.GetView();
        }

        public long GetChainHeight()
        {
            if (hashesToHeights.IsEmpty) return DataView.GetView().GetChainHeight();
            return Math.Max(DataView.GetView().GetChainHeight(), hashesToHeights.Values.Max());
        }

        public void WriteToBuffer(Block blk)
        {
            _buffer.Enqueue(blk);

            if (_flushEveryBlock)
            {
                lock (buffer)
                {
                    buffer.Add(blk);
                }

                ForceFlush();
            }

            UpdateBuffers(blk);
        }

        public void ForceFlush()
        {
            lock (buffer)
            {
                if (buffer.Count == 0)
                {
                    return;
                }

                Flush(buffer);
                buffer.Clear();
            }
        }

        /// <summary>
        /// Starts the block buffer's flusher.
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            _pIndex = DataView.GetView().GetOutputIndex();

            var timer = new PeriodicTimer(_flushInterval);
            if (_flushEveryBlock)
            {
                return;
            }
            while (await timer.WaitForNextTickAsync())
            {
                lock (buffer)
                {
                    while (_buffer.TryDequeue(out var block))
                    {
                        buffer.Add(block);
                    }

                    if (buffer.Count == 0)
                    {
                        continue;
                    }

                    Flush(buffer);
                    buffer.Clear();
                }
            }
        }

        public IEnumerable<Block> GetBlocks(long startHeight, long limit) => _view.GetBlocks(startHeight, limit);

        public void AddBlockToCache(Block blk) => _view.AddBlockToCache(blk);

        public bool BlockCacheHas(SHA256 blk) => _view.BlockCacheHas(blk);

        public void ClearBlockCache() => _view.ClearBlockCache();

        public void AddBlock(Block blk) => _buffer.Enqueue(blk);

        public Dictionary<long, Block> GetBlockCache() => _view.GetBlockCache();

        public void RemovePubOutput(TTXInput inp) => _view.RemovePubOutput(inp);

        public uint[] GetOutputIndices(SHA256 hash) => _view.GetOutputIndices(hash);

        public (TXOutput[], int) GetMixins(uint idx) => _view.GetMixins(idx);

        public (TXOutput[], int) GetMixinsUniform(uint idx) => _view.GetMixinsUniform(idx);

        public FullTransaction GetTransaction(SHA256 hash)
        {
            try
            {
                return _view.GetTransaction(hash);
            }
            catch(Exception e)
            {
                try
                {
                    return transactionCache[hash];
                }
                catch
                {
                    throw e;
                }
            }
        }

        public FullTransaction GetTransaction(ulong txid) => _view.GetTransaction(txid);

        public Block GetBlock(long height)
        {
            try
            {
                return _view.GetBlock(height);
            }
            catch (Exception e)
            {
                var success = blockCache.TryGetValue(height, out var blk);
                if (!success)
                {
                    throw;
                }

                if (blk == null) throw;
                return blk;
            }
        }

        public Block GetBlock(SHA256 hash)
        {
            try
            {
                return _view.GetBlock(hash);
            }
            catch (Exception e)
            {
                var success = hashesToHeights.TryGetValue(hash, out var height);
                if (!success)
                {
                    throw;
                }

                success = blockCache.TryGetValue(height, out var blk);
                if (!success)
                {
                    throw;
                }

                if (blk == null) throw;
                return blk;
            }
        }

        public ulong GetTransactionIndexer() => _view.GetTransactionIndexer();

        public ulong GetTransactionIndex(SHA256 h) => _view.GetTransactionIndex(h);

        public long GetBlockHeight(SHA256 hash)
        {
            try
            {
                return _view.GetBlockHeight(hash);
            }
            catch (Exception e)
            {
                var success = hashesToHeights.TryGetValue(hash, out var height);
                if (!success)
                {
                    throw;
                }

                return height;
            }
        }

        public bool BlockHeightExists(long h)
        {
            lock (buffer)
            {
                return _view.BlockHeightExists(h) || blockCache.Keys.Any(x => x == h);
            }
        }

        public void Flush(IEnumerable<UpdateEntry> updates) => throw new Exception("Discreet.DB.BlockBuffer: cannot directly update the block buffer's database!");

        private void UpdateBuffers(Block block)
        {
            if (blockCache.ContainsKey(block.Header.Height)) return;

            blockCache[block.Header.Height] = block;
            hashesToHeights[block.Header.BlockHash] = block.Header.Height;

            foreach (var tx in block.Transactions)
            {
                transactionCache[tx.TxID] = tx;

                lock (spentKeys)
                {
                    for (int i = 0; i < tx.NumPInputs; i++)
                    {
                        spentKeys.Add(tx.PInputs[i].KeyImage);
                    }
                }

                for (int i = 0; i < tx.NumTInputs; i++)
                {
                    if (inputCache.ContainsKey(tx.TInputs[i]))
                    {
                        inputCache.Remove(tx.TInputs[i], out _);
                    }
                }

                for (int i = 0; i < tx.NumTOutputs; i++)
                {
                    inputCache[new TTXInput { Offset = (byte)i, TxSrc = tx.TxID }] = tx.TOutputs[i];
                }

                lock (_pLock)
                {
                    for (int i = 0; i < tx.NumPOutputs; i++)
                    {
                        outputCache[++_pIndex] = tx.POutputs[i];
                    }
                }
            }
        }

        private void Flush(List<Block> _blocks)
        {
            // sort blocks by height
            var blocks = _blocks.OrderBy(x => x.Header.Height).ToList();
            List<UpdateEntry> updates = new List<UpdateEntry>();

            // get previous indexers
            var tIndex = DataView.GetView().GetTransactionIndexer();
            var pIndex = DataView.GetView().GetOutputIndex();
            var bIndex = blocks.Select(x => x.Header.Height).Max();

            Dictionary<TTXInput, TTXOutput> pubUpdates = new Dictionary<TTXInput, TTXOutput>(new TTXInputEqualityComparer());
            Dictionary<long, Block> processed = new();

            foreach (var block in blocks)
            {
                if (DataView.GetView().BlockExists(block.Header.BlockHash) || processed.ContainsKey(block.Header.Height)) continue;

                // add block info
                updates.Add(new UpdateEntry { key = block.Header.BlockHash.Bytes, value = Serialization.Int64(block.Header.Height), rule = UpdateRule.ADD, type = UpdateType.BLOCKHEIGHT });
                updates.Add(new UpdateEntry { key = Serialization.Int64(block.Header.Height), value = block.Serialize(), rule = UpdateRule.ADD, type = UpdateType.BLOCK });
                updates.Add(new UpdateEntry { key = Serialization.Int64(block.Header.Height), value = block.Header.Serialize(), rule = UpdateRule.ADD, type = UpdateType.BLOCKHEADER });

                // add transactions
                foreach (var tx in block.Transactions)
                {
                    tIndex++;
                    updates.Add(new UpdateEntry { key = tx.TxID.Bytes, value = Serialization.UInt64(tIndex), rule = UpdateRule.ADD, type = UpdateType.TXINDEX });
                    updates.Add(new UpdateEntry { key = Serialization.UInt64(tIndex), value = tx.Serialize(), rule = UpdateRule.ADD, type = UpdateType.TX });

                    // pouts
                    uint[] uarr = new uint[tx.NumPOutputs];
                    for (int i = 0; i < tx.NumPOutputs; i++)
                    {
                        pIndex++;
                        updates.Add(new UpdateEntry { key = Serialization.UInt32(pIndex), value = tx.POutputs[i].Serialize(), rule = UpdateRule.ADD, type = UpdateType.OUTPUT });
                        uarr[i] = pIndex;
                    }

                    // pout indices
                    updates.Add(new UpdateEntry { key = tx.TxID.Bytes, value = Serialization.UInt32Array(uarr), rule = UpdateRule.ADD, type = UpdateType.OUTPUTINDICES });

                    // spent keys
                    for (int i = 0; i < tx.NumPInputs; i++)
                    {
                        updates.Add(new UpdateEntry { key = tx.PInputs[i].KeyImage.bytes, value = ChainDB.ZEROKEY, rule = UpdateRule.ADD, type = UpdateType.SPENTKEY });
                    }

                    // tinputs
                    for (int i = 0; i < tx.NumTInputs; i++)
                    {
                        if (pubUpdates.ContainsKey(tx.TInputs[i])) pubUpdates.Remove(tx.TInputs[i]);
                        else updates.Add(new UpdateEntry { key = tx.TInputs[i].Serialize(), value = ChainDB.ZEROKEY, rule = UpdateRule.DEL, type = UpdateType.PUBOUTPUT });
                    }

                    // touts
                    for (int i = 0; i < tx.NumTOutputs; i++)
                    {
                        pubUpdates[new TTXInput { TxSrc = tx.TxID, Offset = (byte)i }] = tx.TOutputs[i];
                    }
                }

                processed[block.Header.Height] = block;
            }

            // new touts
            foreach ((var txi, var txo) in pubUpdates)
            {
                txo.TransactionSrc = txi.TxSrc;
                updates.Add(new UpdateEntry { key = txi.Serialize(), value = txo.Serialize(), rule = UpdateRule.ADD, type = UpdateType.PUBOUTPUT });
            }

            // update indexers
            updates.Add(new UpdateEntry { key = Encoding.ASCII.GetBytes("indexer_tx"), value = Serialization.UInt64(tIndex), rule = UpdateRule.UPDATE, type = UpdateType.TXINDEXER });
            updates.Add(new UpdateEntry { key = Encoding.ASCII.GetBytes("indexer_output"), value = Serialization.UInt32(pIndex), rule = UpdateRule.UPDATE, type = UpdateType.OUTPUTINDEXER });
            updates.Add(new UpdateEntry { key = Encoding.ASCII.GetBytes("height"), value = Serialization.Int64(bIndex), rule = UpdateRule.UPDATE, type = UpdateType.HEIGHT });

            // perform flush
            DataView.GetView().Flush(updates);
            
            lock (_pLock)
            {
                _pIndex = DataView.GetView().GetOutputIndex();
            }

            // dump data
            lock (spentKeys)
            {
                spentKeys.Clear();
            }

            blockCache.Clear();
            inputCache.Clear();
            transactionCache.Clear();
            outputCache.Clear();
            hashesToHeights.Clear();
        }
    }
}
