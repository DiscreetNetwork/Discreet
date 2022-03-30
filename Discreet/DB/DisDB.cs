using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;
using RocksDbSharp;

namespace Discreet.DB
{
    /**
     * The following DB schema is adopted from Monero, with some simplification.
     * 
     * Table            Key             Value
     * -----            ---             -----
     * spent_keys       input hash      -
     * 
     * tx_pool_meta     tx hash         tx metadata
     * tx_pool_blob     tx hash         tx blob
     * 
     * outputs          output index    tx output
     * 
     * tx_indices       tx hash         {tx ID, metadata}
     * txs              tx ID           tx
     * 
     * tx_outputs       tx hash         {array of output indices}
     * owned_outputs    owned index     utxo
     * 
     * block_info       block ID        {block metadata}
     * block_heights    block hash      block height
     * blocks           block ID        block blob
     * cacheblock       block hash      block blob
     * block_headers    block ID        block header
     * 
     * For the sake of efficiency, certain data is duplicated slightly for better retrieval.
     * Spent keys (I.E. linking tags) are duplicated for quick double spend verification.
     * TX Outputs are duplicated (save for TransactionSrc) in transactions/outputs tables for quick use in mixin rings.
     * 
     * For the sake of clarification, a description of "indices" will be used here.
     * Wherever the word "index/indices" is used, I have done my best to clarify specifically what this means in each 
     * context. Usually this will refer to the output index, which is used for quick grabbing of outputs for use in
     * mixins. This is called the Global Index of the output, and is determined when the head block is added to the 
     * database. Since we guarantee a topological sort of transactions via consensus, we can deterministically add 
     * each transaction to the database to get transaction indices and the outputs for each transaction to the 
     * database consistently to get the global indices for each output. As such, global index and transaction index
     * information is consistent among all honest nodes (at the cost of availability and partition tolerance); yet,
     * validation/consensus regains availablility and partition tolerance (CAP theorem). 
     * 
     * A note on DAGs:
     * We currently do not have Aleph implemented to the point where we are ready to use it for testnet. As such,
     * database information regarding the block DAG is not present. Once consensus implementation matures, such changes
     * will be reflected in this file. For now, we support a blockchain for the sake of quick development towards a
     * viable testnet.
     * 
     * Another note:
     * Block metadata is currently not being tracked since testnet blocks will all be minted by similar parties.
     * Same goes for transaction metadata for now.
     * For tx_indices, we only store tx ID. 
     */
    public class U64
    {
        public ulong Value;

        public U64(ulong value)
        {
            Value = value;
        }
    }

    public class L64
    {
        public long Value;

        public L64(long value)
        {
            Value = value;
        }
    }

    public class U32
    {
        public uint Value;

        public U32(uint value)
        {
            Value = value;
        }
    }

    /**
     * New implementation of kvstore backend using RocksDbSharp
     */
    public class DisDB
    {
        private static DisDB disdb;

        private static object disdb_lock = new object();

        public static DisDB GetDB()
        {
            lock (disdb_lock)
            {
                if (disdb == null) Initialize();

                return disdb;
            }
        }

        public static void Initialize()
        {
            lock (disdb_lock)
            {
                if (disdb == null)
                {
                    disdb = new DisDB(Daemon.DaemonConfig.GetDefault().DBPath);
                }
            }
        }

        private static readonly object db_update_lock = new object();

        public static object DBLock { get { return db_update_lock; } }

        public static ColumnFamilyHandle SpentKeys;
        public static ColumnFamilyHandle TxPoolBlob;
        public static ColumnFamilyHandle TxPoolSpentKeys;
        public static ColumnFamilyHandle Outputs;
        public static ColumnFamilyHandle TxIndices;
        public static ColumnFamilyHandle Txs;
        public static ColumnFamilyHandle BlockHeights;
        public static ColumnFamilyHandle Blocks;
        public static ColumnFamilyHandle Meta;
        public static ColumnFamilyHandle OutputIndices;        
        public static ColumnFamilyHandle BlockCache;
        public static ColumnFamilyHandle BlockHeaders;

        public static string SPENT_KEYS = "spent_keys";
        public static string TX_POOL_BLOB = "tx_pool_blob";
        public static string TX_POOL_SPENT_KEYS = "tx_pool_spent_keys";
        public static string OUTPUTS = "outputs";
        public static string OUTPUT_INDICES = "output_indices";
        public static string TX_INDICES = "tx_indices";
        public static string TXS = "txs";
        public static string BLOCK_HEIGHTS = "block_heights";
        public static string BLOCKS = "blocks";
        public static string META = "meta";
        public static string BLOCK_CACHE = "block_cache";
        public static string BLOCK_HEADERS = "block_headers";

        /* zero key */
        public static byte[] ZEROKEY = new byte[8];

        private RocksDb db;

        public string Folder
        {
            get { return folder; }
        }

        private string folder;

        private U64 indexer_tx = new U64(0);
        private U32 indexer_output = new U32(0);

        private L64 height = new L64(-1);

        public DisDB(string path)
        {
            try
            {
                if (File.Exists(path)) throw new Exception("Discreet.DisDB: expects a valid directory path, not a file");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var options = new DbOptions().SetCreateIfMissing().SetCreateMissingColumnFamilies().SetKeepLogFileNum(5);

                var _colFamilies = new ColumnFamilies
                {
                    new ColumnFamilies.Descriptor(SPENT_KEYS, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(TX_POOL_BLOB, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(TX_POOL_SPENT_KEYS, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(OUTPUTS, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(TX_INDICES, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(TXS, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(BLOCK_HEIGHTS, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(BLOCKS, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(META, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(OUTPUT_INDICES, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(BLOCK_CACHE, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(BLOCK_HEADERS, new ColumnFamilyOptions()),
                };

                db = RocksDb.Open(options, path, _colFamilies);

                SpentKeys = db.GetColumnFamily(SPENT_KEYS);
                TxPoolBlob = db.GetColumnFamily(TX_POOL_BLOB);
                TxPoolSpentKeys = db.GetColumnFamily(TX_POOL_SPENT_KEYS);
                Outputs = db.GetColumnFamily(OUTPUTS);
                TxIndices = db.GetColumnFamily(TX_INDICES);
                Txs = db.GetColumnFamily(TXS);
                BlockHeights = db.GetColumnFamily(BLOCK_HEIGHTS);
                Blocks = db.GetColumnFamily(BLOCKS);
                Meta = db.GetColumnFamily(META);
                OutputIndices = db.GetColumnFamily(OUTPUT_INDICES);
                BlockCache = db.GetColumnFamily(BLOCK_CACHE);
                BlockHeaders = db.GetColumnFamily(BLOCK_HEADERS);

                if (db.Get(Encoding.ASCII.GetBytes("meta"), cf: Meta) == null)
                {
                    /* completely empty and has just been created */
                    db.Put(Encoding.ASCII.GetBytes("meta"), ZEROKEY, cf: Meta);
                    db.Put(Encoding.ASCII.GetBytes("indexer_tx"), Serialization.UInt64(indexer_tx.Value), cf: Meta);
                    db.Put(Encoding.ASCII.GetBytes("indexer_output"), Serialization.UInt32(indexer_output.Value), cf: Meta);
                    db.Put(Encoding.ASCII.GetBytes("height"), Serialization.Int64(height.Value), cf: Meta);
                }
                else
                {
                    var result = db.Get(Encoding.ASCII.GetBytes("indexer_tx"), cf: Meta);

                    if (result == null)
                    {
                        throw new Exception($"Discreet.DisDB: Fatal error: could not get indexer_tx");
                    }

                    indexer_tx.Value = Serialization.GetUInt64(result, 0);

                    result = db.Get(Encoding.ASCII.GetBytes("indexer_output"), cf: Meta);

                    if (result == null)
                    {
                        throw new Exception($"Discreet.DisDB: Fatal error: could not get indexer_output");
                    }

                    indexer_output.Value = Serialization.GetUInt32(result, 0);

                    result = db.Get(Encoding.ASCII.GetBytes("height"), cf: Meta);

                    if (result == null)
                    {
                        throw new Exception($"Discreet.DisDB: Fatal error: could not get height");
                    }

                    height.Value = Serialization.GetInt64(result, 0);
                }

                folder = path;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Fatal($"failed to create the database: {ex}");
            }
        }

        public bool IsOpen()
        {
            return db != null;
        }

        public void MustBeOpen()
        {
            if (!IsOpen()) throw new Exception("Discreet.DisDB.DisDB: database is not open.");
        }

        /** 
         * <summary>Disposes of all data in the database, then disposes of the database class.<br />WARNING: DATA WILL NOT BE RECOVERED AND THE BLOCKCHAIN WILL BE LOST.</summary>
         */
        public void DropAll()
        {
            MustBeOpen();

            db.DropColumnFamily(SPENT_KEYS);
            db.DropColumnFamily(TX_POOL_BLOB);
            db.DropColumnFamily(TX_POOL_SPENT_KEYS);
            db.DropColumnFamily(OUTPUTS);
            db.DropColumnFamily(TX_INDICES);
            db.DropColumnFamily(TXS);
            db.DropColumnFamily(BLOCK_HEIGHTS);
            db.DropColumnFamily(BLOCKS);
            db.DropColumnFamily(META);
            db.DropColumnFamily(OUTPUT_INDICES);
            db.DropColumnFamily(BLOCK_CACHE);
            db.DropColumnFamily(BLOCK_HEADERS);

            disdb = null;
            db.Dispose();
            db = null; 
        }

        /**
         * <summary>Adds a block to the cache. Does not assume block has been verified (which it shouldn't be).</summary>
         */
        public void AddBlockToCache(Block blk)
        {
            var result = db.Get(Serialization.Int64(blk.Header.Height), cf: BlockHeights);

            if (result != null)
            {
                throw new Exception($"Discreet.DisDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} (height {blk.Header.Height}) already in database!");
            }

            if (blk.Transactions == null || blk.Transactions.Length == 0 || blk.Header.NumTXs == 0)
            {
                throw new Exception($"Discreet.DisDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} has no transactions!");
            }

            if ((long)blk.Header.Timestamp > DateTime.UtcNow.AddHours(2).Ticks)
            {
                throw new Exception($"Discreet.DisDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} from too far in the future!");
            }

            /* unfortunately, we can't check the transactions yet, since some output indices might not be present. We check a few things though. */
            foreach (FullTransaction tx in blk.Transactions)
            {
                if ((!tx.HasInputs() || !tx.HasOutputs()) && (tx.Version != 0))
                {
                    throw new Exception($"Discreet.DisDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} has a transaction without inputs or outputs!");
                }
            }

            if (blk.GetMerkleRoot() != blk.Header.MerkleRoot)
            {
                throw new Exception($"Discreet.DisDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} has invalid Merkle root");
            }

            if (!blk.CheckSignature())
            {
                throw new Exception($"Discreet.DisDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} has missing or invalid signature!");
            }

            db.Put(blk.Header.BlockHash.Bytes, blk.Serialize(), cf: BlockCache);
        }

        public bool BlockCacheHas(Cipher.SHA256 block)
        {
            return db.Get(block.Bytes, cf: BlockCache) != null;
        }

        public void AddBlock(Block blk)
        {
            var result = db.Get(Serialization.Int64(blk.Header.Height), cf: Blocks);

            if (result != null)
            {
                throw new Exception($"Discreet.DisDB.AddBlock: Block {blk.Header.BlockHash.ToHexShort()} (height {blk.Header.Height}) already in database!");
            }

            if (blk.Header.Height > 0)
            {
                result = db.Get(blk.Header.PreviousBlock.Bytes, cf: BlockHeights);

                if (result == null)
                {
                    throw new Exception($"Discreet.DisDB.AddBlock: Previous block hash {blk.Header.PreviousBlock.ToHexShort()} for block {blk.Header.BlockHash.ToHexShort()} (height {blk.Header.Height}) not found");
                }

                long prevBlockHeight = Serialization.GetInt64(result, 0);
                if (prevBlockHeight != height.Value)
                {
                    throw new Exception($"Discreet.DisDB.AddBlock: Previous block hash {blk.Header.PreviousBlock.ToHexShort()} for block {blk.Header.BlockHash.ToHexShort()} (height {blk.Header.Height}) not previous one in sequence (at height {prevBlockHeight})");
                }
            }

            lock (height)
            {
                if (blk.Header.Height != height.Value + 1)
                {
                    throw new Exception($"Discreeet.DB.AddBlock: block height {blk.Header.Height} not in sequence!");
                }

                height.Value++;
            }

            if (blk.Header.NumTXs != blk.Transactions.Length)
            {
                throw new Exception($"Discreet.DisDB.AddBlock: NumTXs field not equal to length of Transaction array in block ({blk.Header.NumTXs} != {blk.Transactions.Length})");
            }

            for (int i = 0; i < blk.Header.NumTXs; i++)
            {
                AddTransaction(blk.Transactions[i]);
            }


            lock (indexer_tx)
            {
                db.Put(Encoding.ASCII.GetBytes("indexer_tx"), Serialization.UInt64(indexer_tx.Value), cf: Meta);
            }

            lock (indexer_output)
            {
                db.Put(Encoding.ASCII.GetBytes("indexer_output"), Serialization.UInt32(indexer_output.Value), cf: Meta);
            }

            lock (height)
            {
                db.Put(Encoding.ASCII.GetBytes("height"), Serialization.Int64(height.Value), cf: Meta);
            }

            db.Put(blk.Header.BlockHash.Bytes, Serialization.Int64(blk.Header.Height), cf: BlockHeights);
            db.Put(Serialization.Int64(blk.Header.Height), blk.Serialize(), cf: Blocks);

            db.Put(Serialization.Int64(blk.Header.Height), blk.Header.Serialize(), cf: BlockHeaders);
        }

        private void AddTransaction(FullTransaction tx)
        {
            Cipher.SHA256 txhash = tx.Hash();
            if (db.Get(txhash.Bytes, cf: TxIndices) != null)
            {
                throw new Exception($"Discreet.DisDB.AddTransaction: Transaction {txhash.ToHexShort()} already in TX table!");
            }

            ulong txIndex;

            lock (indexer_tx)
            {
                indexer_tx.Value++;
                txIndex = indexer_tx.Value;
            }

            db.Put(txhash.Bytes, Serialization.UInt64(txIndex), cf: TxIndices);
            byte[] txraw = tx.Serialize();
            db.Put(Serialization.UInt64(txIndex), txraw, cf: Txs);

            uint[] outputIndices = new uint[tx.NumPOutputs];
            for (int i = 0; i < tx.NumPOutputs; i++)
            {
                outputIndices[i] = AddOutput(tx.POutputs[i]);
            }

            byte[] uintArr = Serialization.UInt32Array(outputIndices);
            db.Put(txhash.Bytes, uintArr, cf: OutputIndices);

            for (int i = 0; i < tx.NumPInputs; i++)
            {
                db.Put(tx.PInputs[i].KeyImage.bytes, ZEROKEY, cf: SpentKeys);
            }
        }

        private uint AddOutput(TXOutput output)
        {
            uint outputIndex;

            lock (indexer_output)
            {
                indexer_output.Value++;
                outputIndex = indexer_output.Value;
            }

            db.Put(Serialization.UInt32(outputIndex), output.Serialize(), cf: Outputs);

            return outputIndex;
        }

        public bool TXPoolContains(Cipher.SHA256 txhash)
        {
            return db.Get(txhash.Bytes, cf: TxPoolBlob) != null;
        }

        public FullTransaction GetTXFromPool(Cipher.SHA256 txhash)
        {
            var result = db.Get(txhash.Bytes, cf: TxPoolBlob);

            if (result == null)
            {
                throw new Exception($"Discreet.DisDB.GetTXFromPool: database get exception: could not get tx {txhash.ToHexShort()}");
            }

            FullTransaction tx = new FullTransaction();
            tx.Deserialize(result);

            return tx;
        }

        public FullTransaction[] GetTXsFromPool(Cipher.SHA256[] txhashs)
        {
            FullTransaction[] txs = new FullTransaction[txhashs.Length];

            for (int i = 0; i < txhashs.Length; i++)
            {
                var result = db.Get(txhashs[i].Bytes, cf: TxPoolBlob);

                if (result == null)
                {
                    throw new Exception($"Discreet.DisDB.GetTXsFromPool: database get exception: could not get tx {txhashs[i].ToHexShort()}");
                }

                FullTransaction tx = new FullTransaction();
                tx.Deserialize(result);

                txs[i] = tx;
            }

            return txs;
        }

        public void UpdateTXPool(IEnumerable<Cipher.SHA256> txhashs)
        {
            foreach (var hash in txhashs)
            {
                db.Remove(hash.Bytes, cf: TxPoolBlob);
            }
        }

        public List<Daemon.TXPool.MemTx> GetTXPool()
        {
            List<Daemon.TXPool.MemTx> pool = new();

            var iterator = db.NewIterator(cf: TxPoolBlob);

            iterator.SeekToFirst();

            while (iterator.Valid())
            {
                var _tx = iterator.Value();
                var tx = new Daemon.TXPool.MemTx();
                tx.Deserialize(_tx);
                pool.Add(tx);

                iterator.Next();
            }

            return pool;
        }

        public Dictionary<long, Block> GetBlockCache()
        {
            Dictionary<long, Block> blockCache = new Dictionary<long, Block>();

            var iterator = db.NewIterator(cf: BlockCache);

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
            db.DropColumnFamily(BLOCK_CACHE);

            BlockCache = db.CreateColumnFamily(new ColumnFamilyOptions(), BLOCK_CACHE);
        }

        public void AddTXToPool(Daemon.TXPool.MemTx tx)
        {
            Cipher.SHA256 txhash = tx.Tx.Hash();

            if (db.Get(txhash.Bytes, cf: TxIndices) != null)
            {
                throw new Exception($"Discreet.DisDB.AddTXToPool: Transaction {txhash.ToHexShort()} already present in TXPool");
            }

            if (tx.Tx.PInputs != null)
            {
                for (int i = 0; i < tx.Tx.NumPInputs; i++)
                {
                    if (!CheckSpentKey(tx.Tx.PInputs[i].KeyImage))
                    {
                        throw new Exception($"Discreet.DisDB.AddTXToPool: Key image {tx.Tx.PInputs[i].KeyImage.ToHexShort()} has already been spent! (double spend)");
                    }

                    if (!CheckSpentKeyBlock(tx.Tx.PInputs[i].KeyImage))
                    {
                        throw new Exception($"Discreet.DisDB.AddTXToPool: Key image {tx.Tx.PInputs[i].KeyImage.ToHexShort()} has already been spent! (double spend)");
                    }
                }

                foreach (var j in tx.Tx.PInputs)
                {
                    db.Put(j.KeyImage.bytes, ZEROKEY, cf: TxPoolSpentKeys);
                }
            }

            db.Put(txhash.Bytes, tx.Serialize(), cf: TxPoolBlob);
        }

        public bool CheckSpentKey(Cipher.Key j)
        {
            return db.Get(j.bytes, cf: TxPoolSpentKeys) == null;
        }

        public bool CheckSpentKeyBlock(Cipher.Key j)
        {
            return db.Get(j.bytes, cf: SpentKeys) == null;
        }

        public uint[] GetOutputIndices(Cipher.SHA256 tx)
        {
            var result = db.Get(tx.Bytes, cf: OutputIndices);

            if (result == null)
            {
                throw new Exception($"Discreet.DisDB.GetOutputIndices: database get exception: could not find with tx {tx.ToHexShort()}");
            }

            return Serialization.GetUInt32Array(result);
        }

        public TXOutput GetOutput(uint index)
        {
            var result = db.Get(Serialization.UInt32(index), cf: Outputs);

            if (result == null)
            {
                throw new Exception($"Discreet.DisDB.GetOutput: No output exists with index {index}");
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

                var result = db.Get(key, cf: Outputs);

                if (result == null)
                {
                    throw new Exception($"Discreet.DisDB.GetMixins: could not get output at index {index[i]}");
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

                var result = db.Get(key, cf: Outputs);

                if (result == null)
                {
                    throw new Exception($"Discreet.DisDB.GetMixins: could not get output at index {rindex}");
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

                var result = db.Get(key, cf: Outputs);

                if (result == null)
                {
                    throw new Exception($"Discreet.DisDB.GetMixins: could not get output at index {rindex}");
                }

                rv[i] = new TXOutput();
                rv[i].Deserialize(result);
                rv[i].Index = rindex;
                chosen.Add(rindex);
                i++;
            }

            byte[] ikey = Serialization.UInt32(index);
            var iresult = db.Get(ikey, Outputs);

            if (iresult == null)
            {
                throw new Exception($"Discreet.DisDB.GetMixins: could not get output at index {index}");
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

                var result = db.Get(key, cf: Outputs);

                if (result == null)
                {
                    throw new Exception($"Discreet.DisDB.GetMixins: could not get output at index {rindex}");
                }

                rv[i] = new TXOutput();
                rv[i].Deserialize(result);
                rv[i].Index = rindex;
                chosen.Add(rindex);
                i++;
            }

            byte[] ikey = Serialization.UInt32(index);
            var iresult = db.Get(ikey, Outputs);

            if (iresult == null)
            {
                throw new Exception($"Discreet.DisDB.GetMixins: could not get output at index {index}");
            }

            var OutAtIndex = rv[i] = new TXOutput();
            rv[i].Deserialize(iresult);
            rv[i].Index = index;

            /* randomly shuffle */
            var rvEnumerated = rv.OrderBy(x => rng.Next(0, 64)).ToList();
            return (rvEnumerated.ToArray(), rvEnumerated.IndexOf(OutAtIndex));
        }

        public FullTransaction GetTransaction(ulong txid)
        {
            var result = db.Get(Serialization.UInt64(txid), cf: Txs);

            if (result == null)
            {
                throw new Exception($"Discreet.DisDB.GetTransaction: No transaction exists with index {txid}");
            }

            FullTransaction tx = new FullTransaction();
            tx.Deserialize(result);
            return tx;
        }

        public FullTransaction GetTransaction(Cipher.SHA256 txhash)
        {
            var result = db.Get(txhash.Bytes, cf: TxIndices);

            if (result == null)
            {
                throw new Exception($"Discreet.DisDB.GetTransaction: No transaction exists with transaction hash {txhash.ToHexShort()}");
            }

            ulong txid = Serialization.GetUInt64(result, 0);
            return GetTransaction(txid);
        }

        public Block GetBlock(long height)
        {
            var result = db.Get(Serialization.Int64(height), cf: Blocks);

            if (result == null)
            {
                throw new Exception($"Discreet.DisDB.GetBlock: No block exists with height {height}");
            }

            Block blk = new Block();
            blk.Deserialize(result);
            return blk;
        }

        public Block GetBlock(Cipher.SHA256 blockHash)
        {
            var result = db.Get(blockHash.Bytes, cf: BlockHeights);

            if (result == null)
            {
                throw new Exception($"Discreet.DisDB.GetBlock: No block exists with block hash {blockHash.ToHexShort()}");
            }

            long height = Serialization.GetInt64(result, 0);
            return GetBlock(height);
        }

        public BlockHeader GetBlockHeader(Cipher.SHA256 blockHash)
        {
            var result = db.Get(blockHash.Bytes, cf: BlockHeights);

            if (result == null)
            {
                throw new Exception($"Discreet.DisDB.GetBlockHeader: No block header exists with block hash {blockHash.ToHexShort()}");
            }

            long height = Serialization.GetInt64(result, 0);
            return GetBlockHeader(height);
        }

        public BlockHeader GetBlockHeader(long height)
        {
            var result = db.Get(Serialization.Int64(height), cf: BlockHeaders);

            if (result == null)
            {
                throw new Exception($"Discreet.DisDB.GetBlockHeader: No block header exists with height {height}");
            }

            BlockHeader header = new BlockHeader();
            header.Deserialize(result);
            return header;
        }

        public uint GetOutputIndex()
        {
            lock (indexer_output)
            {
                return indexer_output.Value;
            }
        }

        public long GetChainHeight()
        {
            lock (height)
            {
                return height.Value;
            }
        }

        public ulong GetTransactionIndex()
        {
            lock (indexer_tx)
            {
                return indexer_tx.Value;
            }
        }

        public ulong GetTransactionIndex(Cipher.SHA256 txhash)
        {
            var result = db.Get(txhash.Bytes, cf: TxIndices);

            if (result == null)
            {
                throw new Exception($"Discreet.DisDB.GetTransactionIndex: No transaction exists with transaction hash {txhash.ToHexShort()}");
            }

            return Serialization.GetUInt64(result, 0);
        }

        public bool ContainsTransaction(Cipher.SHA256 txhash)
        {
            return db.Get(txhash.Bytes, cf: TxIndices) != null;
        }

        public long GetBlockHeight(Cipher.SHA256 blockHash)
        {
            var result = db.Get(blockHash.Bytes, cf: BlockHeights);

            if (result == null)
            {
                throw new Exception($"Discreet.DisDB.GetBlockHeight: No block exists with block hash {blockHash.ToHexShort()}");
            }

            return Serialization.GetInt64(result, 0);
        }
    }
}
