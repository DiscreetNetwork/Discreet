using Discreet.Cipher;
using Discreet.Coin.Models;
using Microsoft.EntityFrameworkCore.Update;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.DB
{
    public interface IView
    {
        public IEnumerable<Block> GetBlocks(long startHeight, long limit);
        public void AddBlockToCache(Block blk);

        public bool TryAddBlockToCache(Block blk)
        {
            try
            {
                AddBlockToCache(blk);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool BlockCacheHas(SHA256 block);
        public void AddBlock(Block blk);
        public Dictionary<long, Block> GetBlockCache();
        public void ClearBlockCache();
        public bool CheckSpentKey(Key j);
        public ScriptTXOutput GetPubOutput(TTXInput _input);
        public void RemovePubOutput(TTXInput _input);
        public uint[] GetOutputIndices(SHA256 tx);
        public TXOutput GetOutput(uint index);
        public TXOutput[] GetMixins(uint[] index);
        public (TXOutput[], int) GetMixins(uint index);
        public (TXOutput[], int) GetMixinsUniform(uint index);
        public FullTransaction GetTransaction(ulong txid);
        public bool TryGetTransaction(ulong txid, out FullTransaction tx)
        {
            try
            {
                tx = GetTransaction(txid);
                return true;
            }
            catch (Exception ex)
            {
                tx = null;
                return false;
            }
        }
        public FullTransaction GetTransaction(SHA256 txhash);
        public bool TryGetTransaction(SHA256 txhash, out FullTransaction tx)
        {
            try
            {
                tx = GetTransaction(txhash);
                return true;
            }
            catch (Exception ex)
            {
                tx = null;
                return false;
            }
        }
        public Block GetBlock(long height);
        public bool TryGetBlock(long height, out Block block)
        {
            try
            {
                block = GetBlock(height);
                return true;
            }
            catch (Exception ex)
            {
                block = null;
                return false;
            }
        }
        public Block GetBlock(SHA256 blockHash);

        public bool TryGetBlock(SHA256 blockHash, out Block block)
        {
            try
            {
                block = GetBlock(blockHash);
                return true;
            }
            catch (Exception ex)
            {
                block = null;
                return false;
            }
        }
        public BlockHeader GetBlockHeader(SHA256 blockHash);
        public bool TryGetBlockHeader(SHA256 blockHash, out BlockHeader header)
        {
            try
            {
                header = GetBlockHeader(blockHash);
                return true;
            }
            catch (Exception ex)
            {
                header = null;
                return false;
            }
        }
        public BlockHeader GetBlockHeader(long height);
        public bool TryGetBlockHeader(long height, out BlockHeader header)
        {
            try
            {
                header = GetBlockHeader(height);
                return true;
            }
            catch (Exception ex)
            {
                header = null;
                return false;
            }
        }
        public uint GetOutputIndex();
        public long GetChainHeight();
        public ulong GetTransactionIndexer();
        public ulong GetTransactionIndex(SHA256 txhash);
        public bool ContainsTransaction(SHA256 txhash);
        public long GetBlockHeight(SHA256 blockHash);
        public bool BlockExists(SHA256 blockHash);
        public bool BlockHeightExists(long height);
        public void Flush(IEnumerable<UpdateEntry> updates);
        public ScriptTXOutput MustGetPubOutput(TTXInput input)
        {
            try
            {
                return GetPubOutput(input);
            }
            catch
            {
                var tx = GetTransaction(input.TxSrc);
                return tx.TOutputs[input.Offset];
            }
        }
    }
}
