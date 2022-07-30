using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;

namespace Discreet.DB
{
    public class DataView
    {
        private static DataView instance;

        public static DataView GetView()
        {
            return instance;
        }

        static DataView()
        {
            instance = new DataView();
        }

        private CurView curView;

        public DataView()
        {
            curView = new CurView();
        }

        public void AddBlockToCache(Block blk) => curView.AddBlockToCache(blk);

        public bool BlockCacheHas(Cipher.SHA256 block) => curView.BlockCacheHas(block);

        public void AddBlock(Block blk)
        {
            curView.AddBlock(blk);
            curView.AddBlock(blk);
        }

        public Dictionary<long, Block> GetBlockCache() => curView.GetBlockCache();

        public void ClearBlockCache() => curView.ClearBlockCache();

        public bool CheckSpentKey(Cipher.Key j) => curView.CheckSpentKey(j);

        public bool CheckSpentKeyBlock(Cipher.Key j) => curView.CheckSpentKeyBlock(j);

        public Coin.Transparent.TXOutput GetPubOutput(Coin.Transparent.TXInput _input) => curView.GetPubOutput(_input);

        public void RemovePubOutput(Coin.Transparent.TXInput _input) => curView.RemovePubOutput(_input);

        public uint[] GetOutputIndices(Cipher.SHA256 tx) => curView.GetOutputIndices(tx);

        public TXOutput GetOutput(uint index) => curView.GetOutput(index);

        public TXOutput[] GetMixins(uint[] index) => curView.GetMixins(index);

        public (TXOutput[], int) GetMixins(uint index) => curView.GetMixins(index);

        public (TXOutput[], int) GetMixinsUniform(uint index) => curView.GetMixins(index);

        public FullTransaction GetTransaction(ulong txid) => curView.GetTransaction(txid);

        public FullTransaction GetTransaction(Cipher.SHA256 txhash) => curView.GetTransaction(txhash);

        public Block GetBlock(long height) => curView.GetBlock(height);

        public Block GetBlock(Cipher.SHA256 blockHash) => curView.GetBlock(blockHash);

        public BlockHeader GetBlockHeader(Cipher.SHA256 blockHash) => curView.GetBlockHeader(blockHash);

        public BlockHeader GetBlockHeader(long height) => curView.GetBlockHeader(height);

        public uint GetOutputIndex() => curView.GetOutputIndex();

        public long GetChainHeight() => curView.GetChainHeight();

        public ulong GetTransactionIndexer() => curView.GetTransactionIndexer();

        public ulong GetTransactionIndex(Cipher.SHA256 txhash) => curView.GetTransactionIndex(txhash);

        public bool ContainsTransaction(Cipher.SHA256 txhash) => curView.ContainsTransaction(txhash);

        public long GetBlockHeight(Cipher.SHA256 blockHash) => curView.GetBlockHeight(blockHash);

        public bool BlockExists(Cipher.SHA256 blockHash) => curView.BlockExists(blockHash);

        public bool BlockHeightExists(long height) => curView.BlockHeightExists(height);

        public void Flush(IEnumerable<UpdateEntry> updates) => curView.Flush(updates);
    }
}
