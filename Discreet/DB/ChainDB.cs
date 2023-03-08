using Discreet.Coin;
using Discreet.Common;
using RocksDbSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NetMQ.NetMQSelector;

namespace Discreet.DB
{
    public class ChainDB
    {
        public ColumnFamilyHandle Txs;
        public ColumnFamilyHandle TxIndices;
        public ColumnFamilyHandle BlockHeights;
        public ColumnFamilyHandle Blocks;
        public ColumnFamilyHandle BlockHeaders;
        public ColumnFamilyHandle BlockCache;
        public ColumnFamilyHandle SpentKeys;
        public ColumnFamilyHandle Outputs;
        public ColumnFamilyHandle OutputIndices;
        public ColumnFamilyHandle PubOutputs;
        public ColumnFamilyHandle Meta;

        public const string TXS = "txs";
        public const string TX_INDICES = "tx_indices";
        public const string BLOCK_HEIGHTS = "block_heights";
        public const string BLOCKS = "blocks";
        public const string BLOCK_HEADERS = "block_headers";
        public const string BLOCK_CACHE = "block_cache";
        public const string SPENT_KEYS = "spent_keys";
        public const string OUTPUTS = "outputs";
        public const string OUTPUT_INDICES = "output_indices";
        public const string PUB_OUTPUTS = "pub_outputs";
        public const string META = "meta";

        public static byte[] ZEROKEY = new byte[8];

        private RocksDb rdb;

        public string Folder
        {
            get { return folder; }
        }

        private string folder;

        private U64 indexer_tx = new U64(0);
        private U32 indexer_output = new U32(0);
        private L64 height = new L64(-1);

        /* write locks */
        private object block_cache_lock = new();

        public bool MetaExists()
        {
            try
            {
                return rdb.Get(Encoding.ASCII.GetBytes("meta"), cf: Meta) != null;
            }
            catch
            {
                return false;
            }
        }

        public ChainDB(string path)
        {
            try
            {
                if (File.Exists(path)) throw new Exception("ArchiveDB: expects a valid directory path, not a file");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var options = new DbOptions().SetCreateIfMissing().SetCreateMissingColumnFamilies().SetKeepLogFileNum(5).SetKeepLogFileNum(5).SetMaxTotalWalSize(5UL * 1048576000UL);

                var _colFamilies = new ColumnFamilies
                {
                    new ColumnFamilies.Descriptor(TXS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(TX_INDICES, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(BLOCK_HEIGHTS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(BLOCKS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(BLOCK_CACHE, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(BLOCK_HEADERS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(SPENT_KEYS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(OUTPUTS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(OUTPUT_INDICES, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(PUB_OUTPUTS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(META, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                };

                rdb = RocksDb.Open(options, path, _colFamilies);

                Txs = rdb.GetColumnFamily(TXS);
                TxIndices = rdb.GetColumnFamily(TX_INDICES);
                BlockHeights = rdb.GetColumnFamily(BLOCK_HEIGHTS);
                Blocks = rdb.GetColumnFamily(BLOCKS);
                BlockCache = rdb.GetColumnFamily(BLOCK_CACHE);
                BlockHeaders = rdb.GetColumnFamily(BLOCK_HEADERS);
                SpentKeys = rdb.GetColumnFamily(SPENT_KEYS);
                Outputs = rdb.GetColumnFamily(OUTPUTS);
                OutputIndices = rdb.GetColumnFamily(OUTPUT_INDICES);
                PubOutputs = rdb.GetColumnFamily(PUB_OUTPUTS);
                Meta = rdb.GetColumnFamily(META);

                if (!MetaExists())
                {
                    /* completely empty and has just been created */
                    rdb.Put(Encoding.ASCII.GetBytes("meta"), ZEROKEY, cf: Meta);
                    rdb.Put(Encoding.ASCII.GetBytes("indexer_tx"), Serialization.UInt64(indexer_tx.Value), cf: Meta);
                    rdb.Put(Encoding.ASCII.GetBytes("indexer_output"), Serialization.UInt32(indexer_output.Value), cf: Meta);
                    rdb.Put(Encoding.ASCII.GetBytes("height"), Serialization.Int64(height.Value), cf: Meta);
                }
                else
                {
                    var result = rdb.Get(Encoding.ASCII.GetBytes("indexer_tx"), cf: Meta);

                    if (result == null)
                    {
                        throw new Exception($"ArchiveDB: Fatal error: could not get indexer_tx");
                    }

                    indexer_tx.Value = Serialization.GetUInt64(result, 0);

                    result = rdb.Get(Encoding.ASCII.GetBytes("indexer_output"), cf: Meta);

                    if (result == null)
                    {
                        throw new Exception($"StateDB: Fatal error: could not get indexer_output");
                    }

                    indexer_output.Value = Serialization.GetUInt32(result, 0);

                    result = rdb.Get(Encoding.ASCII.GetBytes("height"), cf: Meta);

                    if (result == null)
                    {
                        throw new Exception($"ArchiveDB: Fatal error: could not get height");
                    }

                    height.Value = Serialization.GetInt64(result, 0);
                }

                folder = path;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Fatal($"ArchiveDB: failed to create the database: {ex}");
            }
        }

        public IEnumerable<Block> GetBlocks(long startHeight, long limit)
        {
            if (limit <= 0) limit = long.MaxValue;

            var iter = rdb.NewIterator(cf: Blocks);
            iter.SeekToFirst();
            iter.Seek(Serialization.Int64(startHeight));
            while (iter.Valid() && limit > 0)
            {
                Block block = new();
                block.Deserialize(iter.Value());
                iter = iter.Next();
                limit--;
                yield return block;
            }

            if (!iter.Valid()) iter.Dispose();
        }

        /// <summary>
        /// <warn>DEPRACATED</warn>
        /// </summary>
        /// <param name="blk"></param>
        /// <exception cref="Exception"></exception>
        public void AddBlockToCache(Block blk)
        {
            lock (block_cache_lock)
            {
                var result = rdb.Get(Serialization.Int64(blk.Header.Height), cf: BlockHeights);

                if (result != null)
                {
                    throw new Exception($"Discreet.ArchiveDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} (height {blk.Header.Height}) already in database!");
                }

                if (blk.Transactions == null || blk.Transactions.Length == 0 || blk.Header.NumTXs == 0)
                {
                    throw new Exception($"Discreet.ArchiveDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} has no transactions!");
                }

                if ((long)blk.Header.Timestamp > DateTime.UtcNow.AddHours(2).Ticks)
                {
                    throw new Exception($"Discreet.ArchiveDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} from too far in the future!");
                }

                /* unfortunately, we can't check the transactions yet, since some output indices might not be present. We check a few things though. */
                foreach (FullTransaction tx in blk.Transactions)
                {
                    if ((!tx.HasInputs() || !tx.HasOutputs()) && (tx.Version != 0))
                    {
                        throw new Exception($"Discreet.ArchiveDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} has a transaction without inputs or outputs!");
                    }
                }

                if (blk.GetMerkleRoot() != blk.Header.MerkleRoot)
                {
                    throw new Exception($"Discreet.ArchiveDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} has invalid Merkle root");
                }

                if (!blk.CheckSignature())
                {
                    throw new Exception($"Discreet.ArchiveDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} has missing or invalid signature!");
                }

                rdb.Put(blk.Header.BlockHash.Bytes, blk.Serialize(), cf: BlockCache);
            }
        }

        public bool BlockCacheHas(Cipher.SHA256 block)
        {
            return rdb.Get(block.Bytes, cf: BlockCache) != null;
        }

        public void AddBlock(Block blk)
        {
            var result = rdb.Get(Serialization.Int64(blk.Header.Height), cf: Blocks);

            if (result != null)
            {
                throw new Exception($"Discreet.ArchiveDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} (height {blk.Header.Height}) already in database!");
            }

            if (blk.Header.Height > 0)
            {
                result = rdb.Get(blk.Header.PreviousBlock.Bytes, cf: BlockHeights);

                if (result == null)
                {
                    throw new Exception($"Discreet.ArchiveDB.AddBlock: Previous block hash {blk.Header.PreviousBlock.ToHexShort()} for block {blk.Header.BlockHash.ToHexShort()} (height {blk.Header.Height}) not found");
                }

                long prevBlockHeight = Serialization.GetInt64(result, 0);
                if (prevBlockHeight != height.Value)
                {
                    throw new Exception($"Discreet.ArchiveDB.AddBlock: Previous block hash {blk.Header.PreviousBlock.ToHexShort()} for block {blk.Header.BlockHash.ToHexShort()} (height {blk.Header.Height}) not previous one in sequence (at height {prevBlockHeight})");
                }
            }

            lock (height)
            {
                if (blk.Header.Height != height.Value + 1)
                {
                    throw new Exception($"Discreeet.ArchiveDB.AddBlock: block height {blk.Header.Height} not in sequence!");
                }

                height.Value++;
            }

            if (blk.Header.NumTXs != blk.Transactions.Length)
            {
                throw new Exception($"Discreet.ArchiveDB.AddBlock: NumTXs field not equal to length of Transaction array in block ({blk.Header.NumTXs} != {blk.Transactions.Length})");
            }

            for (int i = 0; i < blk.Header.NumTXs; i++)
            {
                AddTransaction(blk.Transactions[i]);
            }

            lock (indexer_tx)
            {
                rdb.Put(Encoding.ASCII.GetBytes("indexer_tx"), Serialization.UInt64(indexer_tx.Value), cf: Meta);
            }

            lock (height)
            {
                rdb.Put(Encoding.ASCII.GetBytes("height"), Serialization.Int64(height.Value), cf: Meta);
            }

            rdb.Put(blk.Header.BlockHash.Bytes, Serialization.Int64(blk.Header.Height), cf: BlockHeights);
            rdb.Put(Serialization.Int64(blk.Header.Height), blk.Serialize(), cf: Blocks);

            rdb.Put(Serialization.Int64(blk.Header.Height), blk.Header.Serialize(), cf: BlockHeaders);

            foreach (var tx in blk.Transactions)
            {
                uint[] outputIndices = new uint[tx.NumPOutputs];
                for (int i = 0; i < tx.NumPOutputs; i++)
                {
                    tx.POutputs[i].TransactionSrc = tx.TxID;
                    outputIndices[i] = AddOutput(tx.POutputs[i]);
                }

                byte[] uintArr = Serialization.UInt32Array(outputIndices);
                rdb.Put(tx.TxID.Bytes, uintArr, cf: OutputIndices);

                for (int i = 0; i < tx.NumPInputs; i++)
                {
                    rdb.Put(tx.PInputs[i].KeyImage.bytes, ZEROKEY, cf: SpentKeys);
                }

                for (int i = 0; i < tx.NumTInputs; i++)
                {
                    rdb.Remove(tx.TInputs[i].Serialize(), cf: PubOutputs);
                }

                for (int i = 0; i < tx.NumTOutputs; i++)
                {
                    tx.TOutputs[i].TransactionSrc = tx.TxID;
                    rdb.Put(new Coin.Transparent.TXInput { TxSrc = tx.TOutputs[i].TransactionSrc, Offset = (byte)i }.Serialize(), tx.TOutputs[i].Serialize(), cf: PubOutputs);
                }
            }

            lock (indexer_output)
            {
                rdb.Put(Encoding.ASCII.GetBytes("indexer_output"), Serialization.UInt32(indexer_output.Value), cf: Meta);
            }
        }

        private void AddTransaction(FullTransaction tx)
        {
            Cipher.SHA256 txhash = tx.TxID;
            if (rdb.Get(txhash.Bytes, cf: TxIndices) != null)
            {
                throw new Exception($"Discreet.ArchiveDB.AddTransaction: Transaction {txhash.ToHexShort()} already in TX table!");
            }

            ulong txIndex;

            lock (indexer_tx)
            {
                indexer_tx.Value++;
                txIndex = indexer_tx.Value;
            }

            rdb.Put(txhash.Bytes, Serialization.UInt64(txIndex), cf: TxIndices);
            byte[] txraw = tx.Serialize();
            rdb.Put(Serialization.UInt64(txIndex), txraw, cf: Txs);
        }

        public Dictionary<long, Block> GetBlockCache()
        {
            Dictionary<long, Block> blockCache = new Dictionary<long, Block>();

            var iterator = rdb.NewIterator(cf: BlockCache);

            iterator.SeekToFirst();

            while (iterator.Valid())
            {
                byte[] bytes = iterator.Value();

                if (bytes[0] == 1 || bytes[0] == 2)
                {
                    Block block = new Block();
                    block.Deserialize(bytes);

                    blockCache.Add(block.Header.Height, block);
                }
                else
                {
                    Block block = new Block();
                    block.Deserialize(bytes);

                    blockCache.Add(block.Header.Height, block);
                }

                iterator.Next();
            }

            return blockCache;
        }

        public void ClearBlockCache()
        {
            rdb.DropColumnFamily(BLOCK_CACHE);

            BlockCache = rdb.CreateColumnFamily(new ColumnFamilyOptions(), BLOCK_CACHE);
        }

        public FullTransaction GetTransaction(ulong txid)
        {
            var result = rdb.Get(Serialization.UInt64(txid), cf: Txs);

            if (result == null)
            {
                throw new Exception($"Discreet.ArchiveDB.GetTransaction: No transaction exists with index {txid}");
            }

            FullTransaction tx = new FullTransaction();
            tx.Deserialize(result);
            return tx;
        }

        public FullTransaction GetTransaction(Cipher.SHA256 txhash)
        {
            var result = rdb.Get(txhash.Bytes, cf: TxIndices);

            if (result == null)
            {
                throw new Exception($"Discreet.ArchiveDB.GetTransaction: No transaction exists with transaction hash {txhash.ToHexShort()}");
            }

            ulong txid = Serialization.GetUInt64(result, 0);
            return GetTransaction(txid);
        }

        public Block GetBlock(long height)
        {
            var result = rdb.Get(Serialization.Int64(height), cf: Blocks);

            if (result == null)
            {
                throw new Exception($"Discreet.ArchiveDB.GetBlock: No block exists with height {height}");
            }

            Block blk = new Block();
            blk.Deserialize(result);
            return blk;
        }

        public Block GetBlock(Cipher.SHA256 blockHash)
        {
            var result = rdb.Get(blockHash.Bytes, cf: BlockHeights);

            if (result == null)
            {
                throw new Exception($"Discreet.ArchiveDB.GetBlock: No block exists with block hash {blockHash.ToHexShort()}");
            }

            long height = Serialization.GetInt64(result, 0);
            return GetBlock(height);
        }

        public BlockHeader GetBlockHeader(Cipher.SHA256 blockHash)
        {
            var result = rdb.Get(blockHash.Bytes, cf: BlockHeights);

            if (result == null)
            {
                throw new Exception($"Discreet.ArchiveDB.GetBlockHeader: No block header exists with block hash {blockHash.ToHexShort()}");
            }

            long height = Serialization.GetInt64(result, 0);
            return GetBlockHeader(height);
        }

        public BlockHeader GetBlockHeader(long height)
        {
            var result = rdb.Get(Serialization.Int64(height), cf: BlockHeaders);

            if (result == null)
            {
                throw new Exception($"Discreet.ArchiveDB.GetBlockHeader: No block header exists with height {height}");
            }

            BlockHeader header = new BlockHeader();
            header.Deserialize(result);
            return header;
        }

        public long GetChainHeight()
        {
            lock (height)
            {
                return height.Value;
            }
        }

        public ulong GetTransactionIndexer()
        {
            lock (indexer_tx)
            {
                return indexer_tx.Value;
            }
        }

        public ulong GetTransactionIndex(Cipher.SHA256 txhash)
        {
            var result = rdb.Get(txhash.Bytes, cf: TxIndices);

            if (result == null)
            {
                throw new Exception($"Discreet.ArchiveDB.GetTransactionIndex: No transaction exists with transaction hash {txhash.ToHexShort()}");
            }

            return Serialization.GetUInt64(result, 0);
        }

        public bool ContainsTransaction(Cipher.SHA256 txhash)
        {
            return rdb.Get(txhash.Bytes, cf: TxIndices) != null;
        }

        public bool BlockExists(Cipher.SHA256 block)
        {
            return rdb.Get(block.Bytes, cf: BlockHeights) != null;
        }

        public bool BlockHeightExists(long height)
        {
            return rdb.Get(Serialization.Int64(height), cf: BlockHeaders) != null;
        }

        public long GetBlockHeight(Cipher.SHA256 blockHash)
        {
            var result = rdb.Get(blockHash.Bytes, cf: BlockHeights);

            if (result == null)
            {
                throw new Exception($"Discreet.ArchiveDB.GetBlockHeight: No block exists with block hash {blockHash.ToHexShort()}");
            }

            return Serialization.GetInt64(result, 0);
        }

        public void Flush(IEnumerable<UpdateEntry> updates)
        {
            WriteBatch batch = new WriteBatch();
            U64 txUpdate = null;
            L64 heightUpdate = null;
            U32 outputUpdate = null;

            // iterate once to remove matching adds/removes
            Dictionary<byte[], UpdateEntry> pubMatches = new(new Cipher.Extensions.ByteArrayEqualityComparer());
            foreach (var update in updates)
            {
                switch (update.type)
                {
                    case UpdateType.PUBOUTPUT:
                        if (update.rule == UpdateRule.ADD)
                        {
                            pubMatches[update.key] = update;
                        }
                        else if (update.rule == UpdateRule.DEL)
                        {
                            if (pubMatches.ContainsKey(update.key))
                            {
                                pubMatches.Remove(update.key);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            foreach (var update in updates)
            {
                switch (update.type)
                {
                    case UpdateType.TXINDEXER:
                        txUpdate = new U64(Math.Max(Serialization.GetUInt64(update.value, 0), (txUpdate == null) ? 0 : txUpdate.Value));
                        break;
                    case UpdateType.HEIGHT:
                        heightUpdate = new L64(Math.Max(Serialization.GetInt64(update.value, 0), (heightUpdate == null) ? 0 : heightUpdate.Value));
                        break;
                    case UpdateType.TX:
                        batch.Put(update.key, update.value, cf: Txs);
                        break;
                    case UpdateType.TXINDEX:
                        batch.Put(update.key, update.value, cf: TxIndices);
                        break;
                    case UpdateType.BLOCKHEADER:
                        batch.Put(update.key, update.value, cf: BlockHeaders);
                        break;
                    case UpdateType.BLOCKHEIGHT:
                        batch.Put(update.key, update.value, cf: BlockHeights);
                        break;
                    case UpdateType.BLOCK:
                        batch.Put(update.key, update.value, cf: Blocks);
                        break;
                    case UpdateType.OUTPUTINDEXER:
                        outputUpdate = new U32(Math.Max(Serialization.GetUInt32(update.value, 0), (outputUpdate == null) ? 0 : outputUpdate.Value));
                        break;
                    case UpdateType.OUTPUT:
                        batch.Put(update.key, update.value, cf: Outputs);
                        break;
                    case UpdateType.OUTPUTINDICES:
                        batch.Put(update.key, update.value, cf: OutputIndices);
                        break;
                    case UpdateType.SPENTKEY:
                        batch.Put(update.key, update.value, cf: SpentKeys);
                        break;
                    case UpdateType.PUBOUTPUT:
                        if (update.rule == UpdateRule.DEL) batch.Delete(update.key, cf: PubOutputs);
                        break;
                    default:
                        throw new ArgumentException("unknown or invalid type given");
                }
            }

            foreach (var kv in pubMatches)
            {
                batch.Put(kv.Key, kv.Value.value, cf: PubOutputs);
            }

            if (outputUpdate != null)
            {
                batch.Put(Encoding.ASCII.GetBytes("indexer_output"), Serialization.UInt32(outputUpdate.Value), cf: Meta);
                lock (indexer_output)
                {
                    indexer_output.Value = outputUpdate.Value;
                }
            }

            if (txUpdate != null)
            {
                batch.Put(Encoding.ASCII.GetBytes("indexer_tx"), Serialization.UInt64(txUpdate.Value), cf: Meta);
                lock (indexer_tx)
                {
                    indexer_tx.Value = txUpdate.Value;
                }
            }

            if (heightUpdate != null)
            {
                batch.Put(Encoding.ASCII.GetBytes("height"), Serialization.Int64(heightUpdate.Value), cf: Meta);
                lock (height)
                {
                    height.Value = heightUpdate.Value;
                }
            }

            rdb.Write(batch);
        }

        public uint AddOutput(TXOutput output)
        {
            uint outputIndex;

            lock (indexer_output)
            {
                indexer_output.Value++;
                outputIndex = indexer_output.Value;
            }

            rdb.Put(Serialization.UInt32(outputIndex), output.Serialize(), cf: Outputs);

            return outputIndex;
        }

        public bool CheckSpentKey(Cipher.Key j)
        {
            return rdb.Get(j.bytes, cf: SpentKeys) == null;
        }

        public Coin.Transparent.TXOutput GetPubOutput(Coin.Transparent.TXInput _input)
        {
            var result = rdb.Get(_input.Serialize(), cf: PubOutputs);

            if (result == null)
            {
                throw new Exception($"Discreet.StateDB.GetPubOutput: database get exception: could not find transparent tx output with index {_input}");
            }

            var txo = new Coin.Transparent.TXOutput();
            txo.Deserialize(result);
            return txo;
        }

        public void RemovePubOutput(Coin.Transparent.TXInput _input)
        {
            rdb.Remove(_input.Serialize(), cf: PubOutputs);
        }

        public uint[] GetOutputIndices(Cipher.SHA256 tx)
        {
            var result = rdb.Get(tx.Bytes, cf: OutputIndices);

            if (result == null)
            {
                throw new Exception($"Discreet.StateDB.GetOutputIndices: database get exception: could not find with tx {tx.ToHexShort()}");
            }

            return Serialization.GetUInt32Array(result);
        }

        public TXOutput GetOutput(uint index)
        {
            var result = rdb.Get(Serialization.UInt32(index), cf: Outputs);

            if (result == null)
            {
                throw new Exception($"Discreet.StateDB.GetOutput: No output exists with index {index}");
            }

            TXOutput output = new TXOutput();
            output.Deserialize(result);
            return output;
        }

        public TXOutput[] GetMixins(uint[] index)
        {
            TXOutput[] rv = new TXOutput[index.Length];

            for (int i = 0; i < index.Length; i++)
            {
                byte[] key = Serialization.UInt32(index[i]);

                var result = rdb.Get(key, cf: Outputs);

                if (result == null)
                {
                    throw new Exception($"Discreet.StateDB.GetMixins: could not get output at index {index[i]}");
                }

                rv[i] = new TXOutput();
                rv[i].Deserialize(result);
            }

            return rv;
        }

        public (TXOutput[], int) GetMixins(uint index)
        {
            TXOutput[] rv = new TXOutput[64];

            uint max = GetOutputIndex();
            Random rng = new Random();
            SortedSet<uint> chosen = new SortedSet<uint>();
            chosen.Add(index);

            int i = 0;

            for (; i < 32;)
            {
                uint rindex = (uint)rng.Next(1, (int)max);
                if (chosen.Contains(rindex)) continue;

                byte[] key = Serialization.UInt32(rindex);

                var result = rdb.Get(key, cf: Outputs);

                if (result == null)
                {
                    throw new Exception($"Discreet.StateDB.GetMixins: could not get output at index {rindex}");
                }

                rv[i] = new TXOutput();
                rv[i].Deserialize(result);
                rv[i].Index = rindex;
                chosen.Add(rindex);
                i++;
            }

            for (; i < 63;)
            {
                double uniformVariate = rng.NextDouble();
                double frac = 3.0 / 4.0 * Math.Sqrt(uniformVariate * (1.0 / 4.0) * (1.0 / 4.0));
                uint rindex = (uint)Math.Floor(frac * max);
                if (chosen.Contains(rindex) || rindex == 0) continue;

                byte[] key = Serialization.UInt32(rindex);

                var result = rdb.Get(key, cf: Outputs);

                if (result == null)
                {
                    throw new Exception($"Discreet.StateDB.GetMixins: could not get output at index {rindex}");
                }

                rv[i] = new TXOutput();
                rv[i].Deserialize(result);
                rv[i].Index = rindex;
                chosen.Add(rindex);
                i++;
            }

            byte[] ikey = Serialization.UInt32(index);
            var iresult = rdb.Get(ikey, Outputs);

            if (iresult == null)
            {
                throw new Exception($"Discreet.StateDB.GetMixins: could not get output at index {index}");
            }

            var OutAtIndex = rv[i] = new TXOutput();
            rv[i].Deserialize(iresult);
            rv[i].Index = index;

            /* randomly shuffle */
            var rvEnumerated = rv.OrderBy(x => rng.Next(0, 64)).ToList();
            return (rvEnumerated.ToArray(), rvEnumerated.IndexOf(OutAtIndex));
        }

        public (TXOutput[], int) GetMixinsUniform(uint index)
        {
            TXOutput[] rv = new TXOutput[64];

            uint max = GetOutputIndex();
            Random rng = new Random();
            SortedSet<uint> chosen = new SortedSet<uint>();
            chosen.Add(index);

            int i = 0;

            for (; i < 63;)
            {
                double uniformVariate = rng.NextDouble();
                double frac = 3.0 / 4.0 * Math.Sqrt(uniformVariate * (1.0 / 4.0) * (1.0 / 4.0));
                uint rindex = (uint)Math.Floor(frac * max);
                if (chosen.Contains(rindex) || rindex == 0) continue;

                byte[] key = Serialization.UInt32(rindex);

                var result = rdb.Get(key, cf: Outputs);

                if (result == null)
                {
                    throw new Exception($"Discreet.StateDB.GetMixins: could not get output at index {rindex}");
                }

                rv[i] = new TXOutput();
                rv[i].Deserialize(result);
                rv[i].Index = rindex;
                chosen.Add(rindex);
                i++;
            }

            byte[] ikey = Serialization.UInt32(index);
            var iresult = rdb.Get(ikey, Outputs);

            if (iresult == null)
            {
                throw new Exception($"Discreet.StateDB.GetMixins: could not get output at index {index}");
            }

            var OutAtIndex = rv[i] = new TXOutput();
            rv[i].Deserialize(iresult);
            rv[i].Index = index;

            /* randomly shuffle */
            var rvEnumerated = rv.OrderBy(x => rng.Next(0, 64)).ToList();
            return (rvEnumerated.ToArray(), rvEnumerated.IndexOf(OutAtIndex));
        }

        public uint GetOutputIndex()
        {
            lock (indexer_output)
            {
                return indexer_output.Value;
            }
        }
    }
}