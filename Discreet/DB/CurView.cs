using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin.Models;

namespace Discreet.DB
{
    public class CurView
    {
        /*private ArchiveDB archiveDB;
        private StateDB stateDB;

        public CurView()
        {
            archiveDB = new ArchiveDB(Path.Join(Daemon.DaemonConfig.GetConfig().DBPath, "archive"));
            stateDB = new StateDB(Path.Join(Daemon.DaemonConfig.GetConfig().DBPath, "state"));
        }

        public void AddBlockToCache(Block blk) => archiveDB.AddBlockToCache(blk);

        public bool BlockCacheHas(Cipher.SHA256 block) => archiveDB.BlockCacheHas(block);

        public void AddBlock(Block blk)
        {
            archiveDB.AddBlock(blk);
            stateDB.AddBlock(blk);
        }

        public Dictionary<long, Block> GetBlockCache() => archiveDB.GetBlockCache();

        public void ClearBlockCache() => archiveDB.ClearBlockCache();

        public bool CheckSpentKey(Cipher.Key j) => stateDB.CheckSpentKey(j);

        public Coin.Transparent.TXOutput GetPubOutput(Coin.Transparent.TXInput _input) => stateDB.GetPubOutput(_input);
    
        public void RemovePubOutput(Coin.Transparent.TXInput _input) => stateDB.RemovePubOutput(_input);

        public uint[] GetOutputIndices(Cipher.SHA256 tx) => stateDB.GetOutputIndices(tx);

        public TXOutput GetOutput(uint index) => stateDB.GetOutput(index);

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

        public bool BlockExists(Cipher.SHA256 blockHash) => archiveDB.BlockExists(blockHash);

        public bool BlockHeightExists(long height) => archiveDB.BlockHeightExists(height);

        public void Flush(IEnumerable<UpdateEntry> updates)
        {
            // split the updates into state and archive
            List<UpdateEntry> stateUpdates = new();
            List<UpdateEntry> archiveUpdates = new();

            foreach (var update in updates)
            {
                switch (update.type) 
                {
                    case UpdateType.NULL:
                        break;
                    case UpdateType.SPENTKEY:
                    case UpdateType.OUTPUTINDICES:
                    case UpdateType.OUTPUT:
                    case UpdateType.PUBOUTPUT:
                    case UpdateType.OUTPUTINDEXER:
                        stateUpdates.Add(update);
                        break;
                    case UpdateType.TX:
                    case UpdateType.TXINDEX:
                    case UpdateType.BLOCKHEADER:
                    case UpdateType.BLOCKHEIGHT:
                    case UpdateType.BLOCK:
                    case UpdateType.TXINDEXER:
                    case UpdateType.HEIGHT:
                        archiveUpdates.Add(update);
                        break;
                    default:
                        throw new ArgumentException("one of the given update rules has unknown type");
                }
            }

            stateDB.Flush(stateUpdates);
            archiveDB.Flush(archiveUpdates);
        }*/

        private ChainDB chainDB;

        public CurView()
        {
            chainDB = new ChainDB(Path.Join(Daemon.DaemonConfig.GetConfig().DBPath, "chain"));
        }

        internal void ForceCloseAndWipe() => chainDB.ForceCloseAndWipe();

        public IEnumerable<Block> GetBlocks(long startHeight, long limit) => chainDB.GetBlocks(startHeight, limit);
        public void AddBlockToCache(Block blk) => chainDB.AddBlockToCache(blk);

        public bool BlockCacheHas(Cipher.SHA256 block) => chainDB.BlockCacheHas(block);

        public void AddBlock(Block blk)
        {
            chainDB.AddBlock(blk);
        }

        public Dictionary<long, Block> GetBlockCache() => chainDB.GetBlockCache();

        public void ClearBlockCache() => chainDB.ClearBlockCache();

        public bool CheckSpentKey(Cipher.Key j) => chainDB.CheckSpentKey(j);

        public ScriptTXOutput GetPubOutput(TTXInput _input) => chainDB.GetPubOutput(_input);

        public void RemovePubOutput(TTXInput _input) => chainDB.RemovePubOutput(_input);

        public uint[] GetOutputIndices(Cipher.SHA256 tx) => chainDB.GetOutputIndices(tx);

        public TXOutput GetOutput(uint index) => chainDB.GetOutput(index);

        public TXOutput[] GetMixins(uint[] index) => chainDB.GetMixins(index);

        public (TXOutput[], int) GetMixins(uint index) => chainDB.GetMixins(index);

        public (TXOutput[], int) GetMixinsUniform(uint index) => chainDB.GetMixins(index);

        public FullTransaction GetTransaction(ulong txid) => chainDB.GetTransaction(txid);

        public FullTransaction GetTransaction(Cipher.SHA256 txhash) => chainDB.GetTransaction(txhash);

        public Block GetBlock(long height) => chainDB.GetBlock(height);

        public Block GetBlock(Cipher.SHA256 blockHash) => chainDB.GetBlock(blockHash);

        public BlockHeader GetBlockHeader(Cipher.SHA256 blockHash) => chainDB.GetBlockHeader(blockHash);

        public BlockHeader GetBlockHeader(long height) => chainDB.GetBlockHeader(height);

        public uint GetOutputIndex() => chainDB.GetOutputIndex();

        public long GetChainHeight() => chainDB.GetChainHeight();

        public ulong GetTransactionIndexer() => chainDB.GetTransactionIndexer();

        public ulong GetTransactionIndex(Cipher.SHA256 txhash) => chainDB.GetTransactionIndex(txhash);

        public bool ContainsTransaction(Cipher.SHA256 txhash) => chainDB.ContainsTransaction(txhash);

        public long GetBlockHeight(Cipher.SHA256 blockHash) => chainDB.GetBlockHeight(blockHash);

        public bool BlockExists(Cipher.SHA256 blockHash) => chainDB.BlockExists(blockHash);

        public bool BlockHeightExists(long height) => chainDB.BlockHeightExists(height);

        public void Flush(IEnumerable<UpdateEntry> updates)
        {
            chainDB.Flush(updates);
        }
    }
}