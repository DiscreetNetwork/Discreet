using Discreet.Cipher;
using Discreet.Coin.Comparers;
using Discreet.Coin.Models;
using Discreet.Common;
using Discreet.Common.Serialize;
using Discreet.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Sandbox
{
    public class SandboxView : IView
    {
        private static SandboxView instance;

        public static SandboxView GetView()
        {
            return instance;
        }

        static SandboxView()
        {
            instance = new SandboxView();
        }

        protected Dictionary<uint, TXOutput> _privateOutputs;
        protected Dictionary<ulong, FullTransaction> _transactions;
        protected Dictionary<SHA256, ulong> _transactionIndices;
        protected Dictionary<SHA256, long> _blockHeights;
        protected Dictionary<long, Block> _blocks;
        protected Dictionary<long, BlockHeader> _blockHeaders;
        protected HashSet<Key> _spentKeys;
        protected Dictionary<TTXInput, ScriptTXOutput> _outputs;
        protected Dictionary<ulong, uint[]> _outputIndices;

        protected long _blockIndex;
        protected ulong _transactionIndex;
        protected uint _outputIndex;

        public SandboxView()
        {
            _privateOutputs = new Dictionary<uint, TXOutput>();
            _transactions = new Dictionary<ulong, FullTransaction>();
            _transactionIndices = new Dictionary<SHA256, ulong>(new SHA256EqualityComparer());
            _blockHeights = new Dictionary<SHA256, long>(new SHA256EqualityComparer());
            _blocks = new Dictionary<long, Block>();
            _blockHeaders = new Dictionary<long, BlockHeader>();
            _spentKeys = new HashSet<Key>(new KeyEqualityComparer());
            _outputs = new Dictionary<TTXInput, ScriptTXOutput>(new TTXInputEqualityComparer());
            _outputIndices = new Dictionary<ulong, uint[]>();

            _blockIndex = -1;
            _transactionIndex = 0;
            _outputIndex = 0;
        }

        public void AddBlock(Block blk)
        {
            _blocks[blk.Header.Height] = blk;
            _blockHeaders[blk.Header.Height] = blk.Header;
            _blockHeights[blk.Header.BlockHash] = blk.Header.Height;
            
            foreach (var tx in blk.Transactions)
            {
                _transactions[_transactionIndex] = tx;
                _transactionIndices[tx.TxID] = _transactionIndex;

                uint[] _outIs = new uint[tx.NumPOutputs];
                for (int i = 0; i < tx.NumPOutputs; i++)
                {
                    _privateOutputs[_outputIndex] = tx.POutputs[i];
                    _outIs[i] = _outputIndex;
                    _outputIndex++;
                }

                _outputIndices[_transactionIndex] = _outIs;

                for (int i = 0; i < tx.NumPInputs; i++)
                {
                    _spentKeys.Add(tx.PInputs[i].KeyImage);
                }

                for (int i = 0; i < tx.NumTInputs; i++)
                {
                    _outputs.Remove(tx.TInputs[i]);
                }

                for (int i = 0; i <= tx.NumTOutputs; i++)
                {
                    _outputs[new TTXInput { TxSrc = tx.TxID, Offset = (byte)i}] = tx.TOutputs[i];
                }

                _transactionIndex++;
            }
        }

        public void AddBlockToCache(Block blk)
        {
            throw new NotImplementedException();
        }

        public bool BlockCacheHas(SHA256 block)
        {
            throw new NotImplementedException();
        }

        public bool BlockExists(SHA256 blockHash)
        {
            return _blockHeights.ContainsKey(blockHash);
        }

        public bool BlockHeightExists(long height)
        {
            return _blocks.ContainsKey(height);
        }

        public bool CheckSpentKey(Key j)
        {
            return !_spentKeys.Contains(j);
        }

        public void ClearBlockCache()
        {
            throw new NotImplementedException();
        }

        public bool ContainsTransaction(SHA256 txhash)
        {
            return _transactionIndices.ContainsKey(txhash);
        }

        public void Flush(IEnumerable<UpdateEntry> updates)
        {
            ulong newTxIndex = _transactionIndex;
            long newBlockIndex = _blockIndex;
            uint newOutputIndex = _outputIndex;

            Dictionary<TTXInput, ScriptTXOutput> _newOutputs = new Dictionary<TTXInput, ScriptTXOutput>(new TTXInputEqualityComparer());

            foreach (var update in updates)
            {
                switch (update.type)
                {
                    case UpdateType.PUBOUTPUT:
                        if (update.rule == UpdateRule.ADD)
                        {
                            _newOutputs[new TTXInput().Deserialize(update.key)] = new ScriptTXOutput().Deserialize(update.value);
                        }
                        else if (update.rule == UpdateRule.DEL)
                        {
                            _newOutputs.Remove(new TTXInput().Deserialize(update.key));
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
                        newTxIndex = Math.Max(Serialization.GetUInt64(update.value, 0), newTxIndex);
                        break;
                    case UpdateType.OUTPUTINDEXER:
                        newOutputIndex = Math.Max(Serialization.GetUInt32(update.value, 0), newOutputIndex);
                        break;
                    case UpdateType.HEIGHT:
                        newBlockIndex = Math.Max(Serialization.GetInt64(update.value, 0), newBlockIndex);
                        break;
                    case UpdateType.TX:
                        _transactions[Serialization.GetUInt64(update.key, 0)] = new FullTransaction().Deserialize(update.value);
                        break;
                    case UpdateType.TXINDEX:
                        _transactionIndices[new SHA256(update.key, false)] = Serialization.GetUInt64(update.value, 0);
                        break;
                    case UpdateType.BLOCKHEADER:
                        _blockHeaders[Serialization.GetInt64(update.key, 0)] = new BlockHeader().Deserialize(update.value);
                        break;
                    case UpdateType.BLOCKHEIGHT:
                        _blockHeights[new SHA256(update.key, false)] = Serialization.GetInt64(update.value, 0);
                        break;
                    case UpdateType.BLOCK:
                        _blocks[Serialization.GetInt64(update.key, 0)] = new Block().Deserialize(update.value);
                        break;
                    case UpdateType.OUTPUT:
                        _privateOutputs[Serialization.GetUInt32(update.key, 0)] = new TXOutput().Deserialize(update.value);
                        break;
                    case UpdateType.OUTPUTINDICES:
                        _outputIndices[Serialization.GetUInt64(update.key, 0)] = Serialization.GetUInt32Array(update.value);
                        break;
                    case UpdateType.SPENTKEY:
                        _spentKeys.Add(new Key(update.key));
                        break;
                    case UpdateType.PUBOUTPUT:
                        if (update.rule == UpdateRule.DEL) _outputs.Remove(new TTXInput().Deserialize(update.key));
                        break;
                    default:
                        break;
                }
            }

            foreach (var kv in _newOutputs)
            {
                _outputs[kv.Key] = kv.Value;
            }

            _transactionIndex = newTxIndex;
            _outputIndex = newOutputIndex;
            _blockIndex = newBlockIndex;
        }

        public Block GetBlock(long height)
        {
            return _blocks[height];
        }

        public Block GetBlock(SHA256 blockHash)
        {
            return _blocks[_blockHeights[blockHash]];
        }

        public Dictionary<long, Block> GetBlockCache()
        {
            throw new NotImplementedException();
        }

        public BlockHeader GetBlockHeader(SHA256 blockHash)
        {
            return _blockHeaders[_blockHeights[blockHash]];
        }

        public BlockHeader GetBlockHeader(long height)
        {
            return _blockHeaders[height];
        }

        public long GetBlockHeight(SHA256 blockHash)
        {
            return _blockHeights[blockHash];
        }

        public IEnumerable<Block> GetBlocks(long startHeight, long limit)
        {
            long _startHeight = Math.Max(startHeight, 0);
            long _limit = Math.Min(limit, _blockIndex);
            while (_startHeight < _limit)
            {
                yield return _blocks[_startHeight++];
            }
        }

        public long GetChainHeight()
        {
            return _blockIndex;
        }

        public TXOutput[] GetMixins(uint[] index)
        {
            return index.Select(x => _privateOutputs[x]).ToArray();
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

                var result = _privateOutputs[rindex];

                if (result == null)
                {
                    throw new Exception($"Discreet.StateDB.GetMixins: could not get output at index {rindex}");
                }

                rv[i] = new TXOutput();
                rv[i].Deserialize(result.Serialize());
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

                var result = _privateOutputs[rindex];

                if (result == null)
                {
                    throw new Exception($"Discreet.StateDB.GetMixins: could not get output at index {rindex}");
                }

                rv[i] = new TXOutput();
                rv[i].Deserialize(result.Serialize());
                rv[i].Index = rindex;
                chosen.Add(rindex);
                i++;
            }

            byte[] ikey = Serialization.UInt32(index);
            var iresult = _privateOutputs[index];

            if (iresult == null)
            {
                throw new Exception($"Discreet.StateDB.GetMixins: could not get output at index {index}");
            }

            var OutAtIndex = rv[i] = new TXOutput();
            rv[i].Deserialize(iresult.Serialize());
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
                uint rindex = (uint)rng.Next(1, (int)max);
                if (chosen.Contains(rindex)) continue;

                var result = _privateOutputs[rindex];

                if (result == null)
                {
                    throw new Exception($"Discreet.StateDB.GetMixins: could not get output at index {rindex}");
                }

                rv[i] = new TXOutput();
                rv[i].Deserialize(result.Serialize());
                rv[i].Index = rindex;
                chosen.Add(rindex);
                i++;
            }

            byte[] ikey = Serialization.UInt32(index);
            var iresult = _privateOutputs[index];

            if (iresult == null)
            {
                throw new Exception($"Discreet.StateDB.GetMixins: could not get output at index {index}");
            }

            var OutAtIndex = rv[i] = new TXOutput();
            rv[i].Deserialize(iresult.Serialize());
            rv[i].Index = index;

            /* randomly shuffle */
            var rvEnumerated = rv.OrderBy(x => rng.Next(0, 64)).ToList();
            return (rvEnumerated.ToArray(), rvEnumerated.IndexOf(OutAtIndex));
        }

        public TXOutput GetOutput(uint index)
        {
            return _privateOutputs[index];
        }

        public uint GetOutputIndex()
        {
            return _outputIndex;
        }

        public uint[] GetOutputIndices(SHA256 tx)
        {
            return _outputIndices[_transactionIndices[tx]];
        }

        public ScriptTXOutput GetPubOutput(TTXInput _input)
        {
            return _outputs[_input];
        }

        public FullTransaction GetTransaction(ulong txid)
        {
            return _transactions[txid];
        }

        public FullTransaction GetTransaction(SHA256 txhash)
        {
            return _transactions[_transactionIndices[txhash]];
        }

        public ulong GetTransactionIndex(SHA256 txhash)
        {
            return _transactionIndices[txhash];
        }

        public ulong GetTransactionIndexer()
        {
            return _transactionIndex;
        }

        public void RemovePubOutput(TTXInput _input)
        {
            _outputs.Remove(_input);
        }
    }
}
