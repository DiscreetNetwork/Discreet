using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;

namespace Discreet.DB
{
    public class CurView
    {
        private ArchiveDB archiveDB;
        private StateDB stateDB;

        private static CurView instance;

        static CurView()
        {
            instance = new CurView();
        }

        public CurView()
        {
            archiveDB = new ArchiveDB(Path.Join(Daemon.DaemonConfig.GetConfig().DBPath, "archive"));
            stateDB = new StateDB(Path.Join(Daemon.DaemonConfig.GetConfig().DBPath, "state"));
        }

        public void AddBlockToCache(Block blk) => archiveDB.AddBlockToCache(blk);

        public void BlockCacheHas(Cipher.SHA256 block) => archiveDB.BlockCacheHas(block);

        public void AddBlock(Block blk)
        {
            archiveDB.AddBlock(blk);
            stateDB.AddBlock(blk);
        }

        public Dictionary<long, Block> GetBlockCache() => archiveDB.GetBlockCache();

        public void ClearBlockCache() => archiveDB.ClearBlockCache();

        public bool CheckSpentKey(Cipher.Key j) => stateDB.CheckSpentKey(j);

        public bool CheckSpentKeyBlock(Cipher.Key j) => stateDB.CheckSpentKeyBlock(j);

        public Coin.Transparent.TXOutput GetPubOutput(Coin.Transparent.TXInput _input) => stateDB.GetPubOutput(_input);
    
        public void RemovePubOutput(Coin.Transparent.TXInput _input) => stateDB?.RemovePubOutput(_input);

        public TXOutput[] GetMixins(uint[] index) => stateDB.GetMixins(index);

        public (TXOutput[], int) GetMixins(uint index) => stateDB.GetMixins(index);

        public (TXOutput[], int) GetMixinsUniform(uint index) => stateDB.GetMixins(index);

        public FullTransaction GetTransaction(ulong txid) => archiveDB.GetTransaction(txid);

        public FullTransaction GetTransaction(Cipher.SHA256 txhash) => archiveDB.GetTransaction(txhash);

        public Block GetBlock(long height) => archiveDB.GetBlock(height);

        public Block GetBlock(Cipher.SHA256 blockHash) => archiveDB.GetBlock(blockHash);

        public BlockHeader GetBlockHeader(Cipher.SHA256 blockHash) => archiveDB.GetBlockHeader(blockHash);

        public BlockHeader GetBlockHeader(long height) => archiveDB.GetBlockHeader(height);

        public uint GetOutputIndex() => stateDB.GetOutputIndex();

        public long GetChainHeight() => archiveDB.GetChainHeight();

        public ulong GetTransactionIndexer() => archiveDB.GetTransactionIndexer();

        public ulong GetTransactionIndex(Cipher.SHA256 txhash) => archiveDB.GetTransactionIndex(txhash);

        public bool ContainsTransaction(Cipher.SHA256 txhash) => archiveDB.ContainsTransaction(txhash);

        public long GetBlockHeight(Cipher.SHA256 blockHash) => archiveDB.GetBlockHeight(blockHash);
    }
}