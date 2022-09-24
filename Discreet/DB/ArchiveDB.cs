using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;
using RocksDbSharp;

namespace Discreet.DB
{
    public class ArchiveDB
    {
        public static ColumnFamilyHandle Txs;
        public static ColumnFamilyHandle TxIndices;
        public static ColumnFamilyHandle BlockHeights;
        public static ColumnFamilyHandle Blocks;
        public static ColumnFamilyHandle BlockHeaders;
        public static ColumnFamilyHandle BlockCache;
        public static ColumnFamilyHandle Meta;

        public const string TXS = "txs";
        public const string TX_INDICES = "tx_indices";
        public const string BLOCK_HEIGHTS = "block_heights";
        public const string BLOCKS = "blocks";
        public const string BLOCK_HEADERS = "block_headers";
        public const string BLOCK_CACHE = "block_cache";
        public const string META = "meta";

        public static byte[] ZEROKEY = new byte[8];

        private RocksDb rdb;

        public string Folder
        {
            get { return folder; }
        }

        private string folder;

        private U64 indexer_tx = new U64(0);
        private L64 height = new L64(-1);

        /* write locks */
        private static object block_cache_lock = new();

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

        public ArchiveDB(string path)
        {
            try
            {
                if (File.Exists(path)) throw new Exception("ArchiveDB: expects a valid directory path, not a file");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var options = new DbOptions().SetCreateIfMissing().SetCreateMissingColumnFamilies().SetKeepLogFileNum(5);

                var _colFamilies = new ColumnFamilies
                {
                    new ColumnFamilies.Descriptor(TXS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(TX_INDICES, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(BLOCK_HEIGHTS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(BLOCKS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(BLOCK_CACHE, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(BLOCK_HEADERS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(META, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                };

                rdb = RocksDb.Open(options, path, _colFamilies);

                Txs = rdb.GetColumnFamily(TXS);
                TxIndices = rdb.GetColumnFamily(TX_INDICES);
                BlockHeights = rdb.GetColumnFamily(BLOCK_HEIGHTS);
                Blocks = rdb.GetColumnFamily(BLOCKS);
                BlockCache = rdb.GetColumnFamily(BLOCK_CACHE);
                BlockHeaders = rdb.GetColumnFamily(BLOCK_HEADERS);
                Meta = rdb.GetColumnFamily(META);

                if (!MetaExists())
                {
                    /* completely empty and has just been created */
                    rdb.Put(Encoding.ASCII.GetBytes("meta"), ZEROKEY, cf: Meta);
                    rdb.Put(Encoding.ASCII.GetBytes("indexer_tx"), Serialization.UInt64(indexer_tx.Value), cf: Meta);
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
                    default:
                        throw new ArgumentException("unknown or invalid type given");
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
    }
}