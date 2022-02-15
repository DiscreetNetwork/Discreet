using System;
using System.Collections.Generic;
using System.Text;
using LightningDB;
using System.Linq;
using System.Threading;
using Discreet.Coin;
using System.IO;

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
     * validation/consensus regains availablility and partition tolerance (CAP theorem shiz). 
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

    public class DB
    {
        private static DB db;

        private static object db_lock = new object();

        public static DB GetDB()
        {
            lock (db_lock)
            {
                if (db == null) Initialize();

                return db;
            }
        }

        public static void Initialize()
        {
            lock (db_lock)
            {
                if (db == null)
                {
                    db = new DB(Visor.VisorConfig.GetDefault().DBPath, Visor.VisorConfig.GetDefault().DBSize);
                }
            }
        }

        private static readonly object db_update_lock = new object();

        public static object DBLock { get { return db_update_lock; } }

        /* table keys */
        public static string SPENT_KEYS = "spent_keys";
        public static string TX_POOL_META = "tx_pool_meta";
        public static string TX_POOL_BLOB = "tx_pool_blob";
        public static string TX_POOL_SPENT_KEYS = "tx_pool_spent_keys";
        public static string OUTPUTS = "outputs";
        public static string TX_INDICES = "tx_indices";
        public static string TXS = "txs";
        public static string BLOCK_INFO = "block_info";
        public static string BLOCK_HEIGHTS = "block_heights";
        public static string BLOCKS = "blocks";
        public static string META = "meta";
        public static string OUTPUT_INDICES = "output_indices";
        public static string OWNED_OUTPUTS = "owned_outputs";
        public static string BLOCK_CACHE = "block_cache";

        /* zero key */
        public static byte[] ZEROKEY = new byte[8];

        /* Environment */
        private LightningEnvironment Env;

        /* Databases */
        private LightningDatabase SpentKeys;
        private LightningDatabase TXPoolMeta;
        private LightningDatabase TXPoolBlob;
        private LightningDatabase TXPoolSpentKeys;
        private LightningDatabase Outputs;
        private LightningDatabase TXIndices;
        private LightningDatabase TXs;
        private LightningDatabase BlockInfo;
        private LightningDatabase BlockHeights;
        private LightningDatabase Blocks;
        private LightningDatabase Meta;
        private LightningDatabase OutputIndices;
        private LightningDatabase OwnedOutputs;
        private LightningDatabase BlockCache;

        public string Folder
        {
            get { return folder; }
        }

        private string folder;

        private U64 indexer_tx = new U64(0);
        private U32 indexer_output = new U32(0);

        private L64 height = new L64(-1);

        private U64 previouslySeenTimestamp = new U64((ulong)DateTime.Now.Ticks);

        private long mapsize = 0;

        private U32 indexer_owned_outputs = new U32(0);

        public long Mapsize 
        {
            get { return mapsize; }
        }


        public void OpenWithSize(string filename, long _mapsize)
        {
            if (Env != null && Env.IsOpened) return;

            if (File.Exists(filename)) throw new Exception("Discreet.DB: Open() expects a valid directory path, not a file");

            if (!Directory.Exists(filename))
            {
                Directory.CreateDirectory(filename);
            }

            Env = new LightningEnvironment(filename, new EnvironmentConfiguration { MaxDatabases = 20 });

            Env.MapSize = _mapsize;

            mapsize = _mapsize;

            Env.Open();
        }

        public void Open(string filename, long dbsize)
        {
            if (Env != null && Env.IsOpened) return;

            if (File.Exists(filename)) throw new Exception("Discreet.DB: Open() expects a valid directory path, not a file");

            if (!Directory.Exists(filename))
            {
                Directory.CreateDirectory(filename);
            }

            Env = new LightningEnvironment(filename, new EnvironmentConfiguration { MaxDatabases = 20 });

            if (Env.MapSize == 0)
            {
                Env.MapSize = dbsize;
            }
            mapsize = Env.MapSize;


            Env.Open();
        }

        /*public void IncreaseDBSize()
        {
            Environment.Dispose();

            mapsize *= 2;

            Open(folder, mapsize);
        }*/

        public DB(string path, long dbsize)
        {
            Open(path, dbsize);

            using var txn = Env.BeginTransaction();
            var config = new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create };

            SpentKeys = txn.OpenDatabase(SPENT_KEYS, config);
            TXPoolMeta = txn.OpenDatabase(TX_POOL_META, config);
            TXPoolBlob = txn.OpenDatabase(TX_POOL_BLOB, config);
            TXPoolSpentKeys = txn.OpenDatabase(TX_POOL_SPENT_KEYS, config);
            Outputs = txn.OpenDatabase(OUTPUTS, config);
            TXIndices = txn.OpenDatabase(TX_INDICES, config);
            TXs = txn.OpenDatabase(TXS, config);
            BlockInfo = txn.OpenDatabase(BLOCK_INFO, config);
            BlockHeights = txn.OpenDatabase(BLOCK_HEIGHTS, config);
            Blocks = txn.OpenDatabase(BLOCKS, config);
            OutputIndices = txn.OpenDatabase(OUTPUT_INDICES, config);
            OwnedOutputs = txn.OpenDatabase(OWNED_OUTPUTS, config);
            BlockCache = txn.OpenDatabase(BLOCK_CACHE, config);

            /* populate our indexers */
            Meta = txn.OpenDatabase(META, config);

            if (!txn.ContainsKey(Meta, Encoding.ASCII.GetBytes("meta")))
            {
                /* completely empty and has just been created */
                txn.Put(Meta, Encoding.ASCII.GetBytes("meta"), ZEROKEY);
                txn.Put(Meta, Encoding.ASCII.GetBytes("indexer_tx"), Serialization.UInt64(indexer_tx.Value));
                txn.Put(Meta, Encoding.ASCII.GetBytes("indexer_output"), Serialization.UInt32(indexer_output.Value));
                txn.Put(Meta, Encoding.ASCII.GetBytes("height"), Serialization.Int64(height.Value));
                txn.Put(Meta, Encoding.ASCII.GetBytes("indexer_owned_outputs"), Serialization.UInt32(indexer_owned_outputs.Value));
            }
            else {
                var result = txn.Get(Meta, Encoding.ASCII.GetBytes("indexer_tx"));

                if (result.resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB: Fatal error: {result.resultCode}");
                }

                indexer_tx.Value = Serialization.GetUInt64(result.value.CopyToNewArray(), 0);

                result = txn.Get(Meta, Encoding.ASCII.GetBytes("indexer_output"));

                if (result.resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB: Fatal error: {result.resultCode}");
                }

                indexer_output.Value = Serialization.GetUInt32(result.value.CopyToNewArray(), 0);

                result = txn.Get(Meta, Encoding.ASCII.GetBytes("height"));

                if (result.resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB: Fatal error: {result.resultCode}");
                }

                height.Value = Serialization.GetInt64(result.value.CopyToNewArray(), 0);

                result = txn.Get(Meta, Encoding.ASCII.GetBytes("indexer_owned_outputs"));

                if (result.resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB: Fatal error: {result.resultCode}");
                }

                indexer_owned_outputs.Value = Serialization.GetUInt32(result.value.CopyToNewArray(), 0);
            }

            folder = path;

            txn.Commit();
        }

        /** 
         * <summary>Disposes of all data in the database, then disposes of the database class.<br />WARNING: DATA WILL NOT BE RECOVERED AND THE BLOCKCHAIN WILL BE LOST.</summary>
         */
        public void DropAll()
        {
            using var txn = Env.BeginTransaction();

            var config = new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create };

            SpentKeys = txn.OpenDatabase(SPENT_KEYS, config);
            TXPoolMeta = txn.OpenDatabase(TX_POOL_META, config);
            TXPoolBlob = txn.OpenDatabase(TX_POOL_BLOB, config);
            TXPoolSpentKeys = txn.OpenDatabase(TX_POOL_SPENT_KEYS, config);
            Outputs = txn.OpenDatabase(OUTPUTS, config);
            TXIndices = txn.OpenDatabase(TX_INDICES, config);
            TXs = txn.OpenDatabase(TXS, config);
            BlockInfo = txn.OpenDatabase(BLOCK_INFO, config);
            BlockHeights = txn.OpenDatabase(BLOCK_HEIGHTS, config);
            Blocks = txn.OpenDatabase(BLOCKS, config);
            OutputIndices = txn.OpenDatabase(OUTPUT_INDICES, config);
            OwnedOutputs = txn.OpenDatabase(OWNED_OUTPUTS, config);
            Meta = txn.OpenDatabase(META, config);
            BlockCache = txn.OpenDatabase(BLOCK_CACHE, config);

            txn.DropDatabase(SpentKeys);
            txn.DropDatabase(TXPoolMeta);
            txn.DropDatabase(TXPoolBlob);
            txn.DropDatabase(TXPoolSpentKeys);
            txn.DropDatabase(Outputs);
            txn.DropDatabase(TXIndices);
            txn.DropDatabase(TXs);
            txn.DropDatabase(BlockInfo);
            txn.DropDatabase(BlockHeights);
            txn.DropDatabase(Blocks);
            txn.DropDatabase(OutputIndices);
            txn.DropDatabase(OwnedOutputs);
            txn.DropDatabase(Meta);
            txn.DropDatabase(BlockCache);

            db = null;
        }

        /**
         * <summary>Adds a block to the cache. Does not assume block has been verified (which it shouldn't be).</summary>
         */
        public void AddBlockToCache(Block blk)
        {
            using var txn = Env.BeginTransaction();

            if (BlockCache == null || !BlockCache.IsOpened)
            {
                BlockCache = txn.OpenDatabase(BLOCK_CACHE);
            }

            if (Blocks == null || !Blocks.IsOpened)
            {
                Blocks = txn.OpenDatabase(BLOCKS);
            }

            var result = txn.Get(Blocks, Serialization.Int64(blk.Height));

            if (result.resultCode == MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddBlock: Block {blk.BlockHash.ToHexShort()} (height {blk.Height}) already in database!");
            }

            if (blk.transactions == null || blk.transactions.Length == 0 || blk.NumTXs == 0)
            {
                throw new Exception($"Discreet.DB.AddBlock: Block {blk.BlockHash.ToHexShort()} has no transactions!");
            }

            if ((long)blk.Timestamp > DateTime.UtcNow.AddHours(2).Ticks)
            {
                throw new Exception($"Discreet.DB.AddBlock: Block {blk.BlockHash.ToHexShort()} from too far in the future!");
            }

            /* unfortunately, we can't check the transactions yet, since some output indices might not be present. We check a few things though. */
            foreach (FullTransaction tx in blk.transactions)
            {
                if ((!tx.HasInputs() || !tx.HasOutputs()) && (tx.Version != 0))
                {
                    throw new Exception($"Discreet.DB.AddBlock: Block {blk.BlockHash.ToHexShort()} has a transaction without inputs or outputs!");
                }
            }

            if (blk.GetMerkleRoot() != blk.MerkleRoot)
            {
                throw new Exception($"Discreet.DB.AddBlock: Block {blk.BlockHash.ToHexShort()} has invalid Merkle root");
            }

            if (blk is SignedBlock block)
            {
                if (!block.CheckSignature())
                {
                    throw new Exception($"Discreet.DB.AddBlock: Block {blk.BlockHash.ToHexShort()} has missing or invalid signature!");
                }
            }

            var resCode = txn.Put(BlockCache, blk.BlockHash.Bytes, blk.SerializeFull());

            if (resCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddBlock: database update exception: {resCode}");
            }
        }

        public bool BlockCacheHas(Cipher.SHA256 block)
        {
            using var txn = Env.BeginTransaction();

            if (BlockCache == null || !BlockCache.IsOpened)
            {
                BlockCache = txn.OpenDatabase(BLOCK_CACHE);
            }

            return txn.ContainsKey(BlockCache, block.Bytes);
        }

        public void AddBlock(Block blk)
        {
            using var txn = Env.BeginTransaction();

            if (BlockHeights == null || !BlockHeights.IsOpened)
            {
                BlockHeights = txn.OpenDatabase(BLOCK_HEIGHTS);
            }

            if (Blocks == null || !Blocks.IsOpened)
            {
                Blocks = txn.OpenDatabase(BLOCKS);
            }

            if (TXPoolBlob == null || !TXPoolBlob.IsOpened)
            {
                TXPoolBlob = txn.OpenDatabase(TX_POOL_BLOB);
            }

            var result = txn.Get(Blocks, Serialization.Int64(blk.Height));

            if (result.resultCode == MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddBlock: Block {blk.BlockHash.ToHexShort()} (height {blk.Height}) already in database!");
            }

            if (blk.Height > 0)
            {
                result = txn.Get(BlockHeights, blk.PreviousBlock.Bytes);

                if (result.resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB.AddBlock: Previous block hash {blk.PreviousBlock.ToHexShort()} for block {blk.BlockHash.ToHexShort()} (height {blk.Height}) not found");
                }

                long prevBlockHeight = Serialization.GetInt64(result.value.CopyToNewArray(), 0);
                if (prevBlockHeight != height.Value)
                {
                    throw new Exception($"Discreet.DB.AddBlock: Previous block hash {blk.PreviousBlock.ToHexShort()} for block {blk.BlockHash.ToHexShort()} (height {blk.Height}) not previous one in sequence (at height {prevBlockHeight})");
                }
            }

            //lock (previouslySeenTimestamp) {
            //    if (previouslySeenTimestamp.Value >= blk.Timestamp)
            //    {
            //        throw new Exception($"Discreet.DB.AddBlock: Block timestamp {blk.Timestamp} not occurring after previously seen timestamp {previouslySeenTimestamp.Value}");
            //    }
            //    previouslySeenTimestamp.Value = blk.Timestamp;
            //}

            lock (height)
            {
                if (blk.Height != height.Value + 1)
                {
                    throw new Exception($"Discreeet.DB.AddBlock: block height {blk.Height} not in sequence!");
                }

                height.Value++;
            }

            var resCode = txn.Put(BlockHeights, blk.BlockHash.Bytes, Serialization.Int64(blk.Height));

            if (resCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddBlock: database update exception: {resCode}");
            }

            resCode = txn.Put(Blocks, Serialization.Int64(blk.Height), blk.SerializeFull());

            if (resCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddBlock: database update exception: {resCode}");
            }

            /*resCode = txn.Commit();

            if (resCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddBlock: database update exception: {resCode}");
            }*/

            if (blk.NumTXs != blk.Transactions.Length)
            {
                throw new Exception($"Discreet.DB.AddBlock: NumTXs field not equal to length of Transaction array in block ({blk.NumTXs} != {blk.Transactions.Length})");
            }

            /* We check if all transactions exist in TXPool; if not, we complain */
            /*for (int i = 0; i < blk.NumTXs; i++)
            {
                if (blk.Height > 0 && !txn.ContainsKey(TXPoolBlob, blk.Transactions[i].Bytes))
                {
                    throw new Exception($"Discreet.DB.AddBlock: Transaction in block {blk.BlockHash.ToHexShort()} ({blk.Transactions[i].ToHexShort()}) not in transaction pool");
                }
            }*/
            /* the above code is unused for now. */

            if (TXIndices == null || !TXIndices.IsOpened)
            {
                TXIndices = txn.OpenDatabase(TX_INDICES);
            }

            if (TXs == null || !TXs.IsOpened)
            {
                TXs = txn.OpenDatabase(TXS);
            }

            if (blk.Height > 0 && blk.transactions == null)
            {
                /* We currently sort by transaction hash to order transactions. */
                /*Cipher.SHA256[] sortedTransactions = new Cipher.SHA256[blk.NumTXs];
                for (int i = 0; i < blk.NumTXs; i++)
                {
                    sortedTransactions[i] = new Cipher.SHA256(blk.Transactions[i].GetBytes(), false);
                }

                for (int i = 0; i < blk.NumTXs - 1; i++)
                {
                    int idx = i;
                    for (int j = i + 1; j < blk.NumTXs; j++)
                    {
                        int cmpval = Cipher.SHA256.Compare(sortedTransactions[j], sortedTransactions[idx]);
                        if (cmpval == 0)
                        {
                            throw new Exception($"Discreet.DB.AddBlock: Duplicate transaction found in block {blk.BlockHash.ToHexShort()} ({blk.Transactions[idx].ToHexShort()})");
                        }
                    }

                    Cipher.SHA256 temp = sortedTransactions[idx];
                    sortedTransactions[idx] = sortedTransactions[i];
                    sortedTransactions[i] = temp;
                }*/

                for (int i = 0; i < blk.NumTXs; i++)
                {
                    AddTransactionFromPool(blk.Transactions[i], txn);
                }
            }
            else
            {
                for (int i = 0; i < blk.NumTXs; i++)
                {
                    AddTransaction(blk.transactions[i], txn);
                }
            }

            /* update indexers */
            if (Meta == null || !Meta.IsOpened)
            {
                Meta = txn.OpenDatabase(META);
            }

            lock (indexer_tx) {
                resCode = txn.Put(Meta, Encoding.ASCII.GetBytes("indexer_tx"), Serialization.UInt64(indexer_tx.Value));

                if (resCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB: Error while updating transaction indexer: {resCode}");
                }
            }

            lock (indexer_output)
            {
                resCode = txn.Put(Meta, Encoding.ASCII.GetBytes("indexer_output"), Serialization.UInt32(indexer_output.Value));

                if (resCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB: Error while updating output indexer: {resCode}");
                }
            }

            lock (height)
            {
                resCode = txn.Put(Meta, Encoding.ASCII.GetBytes("height"), Serialization.Int64(height.Value));

                if (resCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB: Error while updating height indexer: {resCode}");
                }
            }

            resCode = txn.Commit();

            if (resCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddBlock: database update exception: {resCode}");
            }
        }

        private void AddTransaction(FullTransaction tx, LightningTransaction txn)
        {
            Cipher.SHA256 txhash = tx.Hash();

            if (TXIndices == null || !TXIndices.IsOpened)
            {
                TXIndices = txn.OpenDatabase(TX_INDICES);
            }

            if (TXs == null || !TXs.IsOpened)
            {
                TXs = txn.OpenDatabase(TXS);
            }

            if (txn.ContainsKey(TXIndices, txhash.Bytes))
            {
                throw new Exception($"Discreet.DB.AddTransactionFromPool: Transaction {txhash.ToHexShort()} already in TX table!");
            }

            ulong txIndex;

            lock (indexer_tx)
            {
                indexer_tx.Value++;
                txIndex = indexer_tx.Value;
            }

            var resultCode = txn.Put(TXIndices, txhash.Bytes, Serialization.UInt64(txIndex));

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddTransactionFromPool: database update exception: {resultCode}");
            }

            byte[] txraw = tx.Serialize();

            resultCode = txn.Put(TXs, Serialization.UInt64(txIndex), txraw);

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddTransactionFromPool: database update exception: {resultCode}");
            }

            uint[] outputIndices = new uint[tx.NumPOutputs];

            for (int i = 0; i < tx.NumPOutputs; i++)
            {
                outputIndices[i] = AddOutput(tx.POutputs[i], txn);
            }

            byte[] uintArr = Serialization.UInt32Array(outputIndices);

            if (OutputIndices == null || !OutputIndices.IsOpened)
            {
                OutputIndices = txn.OpenDatabase(OUTPUT_INDICES);
            }

            resultCode = txn.Put(OutputIndices, txhash.Bytes, uintArr);

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddTransactionFromPool: database update exception: {resultCode}");
            }

            /* Add spent keys */
            if (SpentKeys == null || !SpentKeys.IsOpened)
            {
                SpentKeys = txn.OpenDatabase(SPENT_KEYS);
            }

            for (int i = 0; i < tx.NumPInputs; i++)
            {
                resultCode = txn.Put(SpentKeys, tx.PInputs[i].KeyImage.bytes, ZEROKEY);

                if (resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB.AddTransactionFromPool: database update exception: {resultCode}");
                }
            }
        }

        private void AddTransactionFromPool(Cipher.SHA256 txhash, LightningTransaction txn)
        {
            /* we've already validated the txhash exists in the tx pool, and the txs are already validated. 
             * So we just add the transactions. We still need to check for duplicates.
             */

            if (TXPoolBlob == null || !TXPoolBlob.IsOpened)
            {
                TXPoolBlob = txn.OpenDatabase(TX_POOL_BLOB);
            }

            if (TXIndices == null || !TXIndices.IsOpened)
            {
                TXIndices = txn.OpenDatabase(TX_INDICES);
            }

            if (TXs == null || !TXs.IsOpened)
            {
                TXs = txn.OpenDatabase(TXS);
            }

            if (txn.ContainsKey(TXIndices, txhash.Bytes))
            {
                throw new Exception($"Discreet.DB.AddTransactionFromPool: Transaction {txhash.ToHexShort()} already in TX table!");
            }

            ulong txIndex;

            lock (indexer_tx)
            {
                indexer_tx.Value++;
                txIndex = indexer_tx.Value;
            }

            var resultCode = txn.Put(TXIndices, txhash.Bytes, Serialization.UInt64(txIndex));

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddTransactionFromPool: database update exception: {resultCode}");
            }

            var result = txn.Get(TXPoolBlob, txhash.Bytes);

            if (result.resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddTransactionFromPool: database update exception: {result.resultCode}");
            }

            byte[] txraw = result.value.CopyToNewArray();

            resultCode = txn.Delete(TXPoolBlob, txhash.Bytes);

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddTransactionFromPool: database update exception: {resultCode}");
            }

            resultCode = txn.Put(TXs, Serialization.UInt64(txIndex), txraw);

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddTransactionFromPool: database update exception: {resultCode}");
            }

            /*resultCode = txn.Commit();

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddTransactionFromPool: database update exception: {resultCode}");
            }*/

            /* Add outputs */
            (TXOutput[] outputs, TXInput[] inputs) = FullTransaction.GetPrivateOutputsAndInputs(txraw);

            uint[] outputIndices = new uint[outputs.Length];

            for (int i = 0; i < outputs.Length; i++)
            {
                outputIndices[i] = AddOutput(outputs[i], txn);
            }

            byte[] uintArr = Serialization.UInt32Array(outputIndices);

            if (OutputIndices == null || !OutputIndices.IsOpened)
            {
                OutputIndices = txn.OpenDatabase(OUTPUT_INDICES);
            }

            resultCode = txn.Put(OutputIndices, txhash.Bytes, uintArr);

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddTransactionFromPool: database update exception: {resultCode}");
            }

            /* Add spent keys */
            if (SpentKeys == null || !SpentKeys.IsOpened)
            {
                SpentKeys = txn.OpenDatabase(SPENT_KEYS);
            }

            for (int i = 0; i < inputs.Length; i++)
            {
                resultCode = txn.Put(SpentKeys, inputs[i].KeyImage.bytes, ZEROKEY);

                if (resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB.AddTransactionFromPool: database update exception: {resultCode}");
                }
            }

            /*resultCode = txn.Commit();

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddTransactionFromPool: database update exception: {resultCode}");
            }*/
        }

        private uint AddOutput(TXOutput output, LightningTransaction txn)
        {
            if (Outputs == null || !Outputs.IsOpened)
            {
                Outputs = txn.OpenDatabase(OUTPUTS);
            }

            uint outputIndex;

            lock (indexer_output)
            {
                indexer_output.Value++;
                outputIndex = indexer_output.Value;
            }

            var resultCode = txn.Put(Outputs, Serialization.UInt32(outputIndex), output.Serialize());

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddOutput: database update exception: {resultCode}");
            }

            return outputIndex;

            /*resultCode = txn.Commit();

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddOutput: database update exception: {resultCode}");
            }*/
        }

        public uint GetTXOutputIndex(LightningTransaction txn, FullTransaction tx, int i)
        {
            if (OutputIndices == null || !OutputIndices.IsOpened)
            {
                OutputIndices = txn.OpenDatabase(OUTPUT_INDICES);
            }

            var result = txn.Get(OutputIndices, tx.Hash().Bytes);

            if (result.resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.GetTXOutputIndex: database get exception: {result.resultCode}");
            }

            uint[] outputIndices = Serialization.GetUInt32Array(result.value.CopyToNewArray());

            return outputIndices[i];
        }

        public (int, Wallets.UTXO) AddWalletOutput(Wallets.WalletAddress addr, FullTransaction tx, int i, bool transparent)
        {
            return AddWalletOutput(addr, tx, i, transparent, false);
        }

        public (int, Wallets.UTXO) AddWalletOutput(Wallets.WalletAddress addr, FullTransaction tx, int i, bool transparent, bool coinbase)
        {
            using var txn = Env.BeginTransaction();

            Wallets.UTXO utxo;

            if (tx.Version == 3)
            {
                utxo = new Wallets.UTXO(tx.TOutputs[i]);
            }
            else if (tx.Version == 4)
            {
                if (transparent)
                {
                    utxo = new Wallets.UTXO(tx.TOutputs[i]);
                }
                else
                {
                    uint index = GetTXOutputIndex(txn, tx, i);
                    utxo = new Wallets.UTXO(addr, index, tx.POutputs[i], tx.ToPrivate(), i, coinbase);
                }
            }
            else
            {
                uint index = GetTXOutputIndex(txn, tx, i);
                utxo = new Wallets.UTXO(addr, index, tx.POutputs[i], tx.ToPrivate(), i, coinbase);
            }

            if (OwnedOutputs == null || !OwnedOutputs.IsOpened)
            {
                OwnedOutputs = txn.OpenDatabase(OWNED_OUTPUTS);
            }

            int outputIndex;

            lock (indexer_owned_outputs)
            {
                indexer_owned_outputs.Value++;
                outputIndex = (int)indexer_owned_outputs.Value;
            }

            var resCode = txn.Put(OwnedOutputs, Serialization.Int32(outputIndex), utxo.Serialize());

            if (resCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddWalletOutput: database update exception: {resCode}");
            }

            lock (indexer_owned_outputs)
            {
                resCode = txn.Put(Meta, Encoding.ASCII.GetBytes("indexer_owned_outputs"), Serialization.UInt32(indexer_owned_outputs.Value));

                if (resCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB: Error while updating height indexer_owned_outputs: {resCode}");
                }
            }

            resCode = txn.Commit();

            if (resCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddWalletOutput: database update exception: {resCode}");
            }

            return (outputIndex, utxo);
        }

        public Wallets.UTXO GetWalletOutput(int index)
        {
            using var txn = Env.BeginTransaction();

            if (OwnedOutputs == null || !OwnedOutputs.IsOpened)
            {
                OwnedOutputs = txn.OpenDatabase(OWNED_OUTPUTS);
            }

            var result = txn.Get(OwnedOutputs, Serialization.Int32(index));

            if (result.resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.GetWalletOutput: database get exception: {result.resultCode}");
            }

            Wallets.UTXO utxo = new Wallets.UTXO();
            utxo.Deserialize(result.value.CopyToNewArray());

            return utxo;
        }

        public bool TXPoolContains(Cipher.SHA256 txhash)
        {
            using var txn = Env.BeginTransaction();

            if (TXPoolBlob == null || !TXPoolBlob.IsOpened)
            {
                TXPoolBlob = txn.OpenDatabase(TX_POOL_BLOB);
            }

            return txn.ContainsKey(TXPoolBlob, txhash.Bytes);
        }

        public FullTransaction GetTXFromPool(Cipher.SHA256 txhash)
        {
            using var txn = Env.BeginTransaction();

            if (TXPoolBlob == null || !TXPoolBlob.IsOpened)
            {
                TXPoolBlob = txn.OpenDatabase(TX_POOL_BLOB);
            }

            if (txn.ContainsKey(TXPoolBlob, txhash.Bytes))
            {
                var result = txn.Get(TXPoolBlob, txhash.Bytes);

                if (result.resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB.GetTXFromPool: database get exception: {result.resultCode}");
                }

                FullTransaction tx = new FullTransaction();
                tx.Deserialize(result.value.CopyToNewArray());

                return tx;
            }

            return null;
        }

        public FullTransaction[] GetTXsFromPool(Cipher.SHA256[] txhashs)
        {
            using var txn = Env.BeginTransaction();

            FullTransaction[] txs = new FullTransaction[txhashs.Length];

            if (TXPoolBlob == null || !TXPoolBlob.IsOpened)
            {
                TXPoolBlob = txn.OpenDatabase(TX_POOL_BLOB);
            }

            for (int i = 0; i < txhashs.Length; i++)
            {
                if (txn.ContainsKey(TXPoolBlob, txhashs[i].Bytes))
                {
                    var result = txn.Get(TXPoolBlob, txhashs[i].Bytes);

                    if (result.resultCode != MDBResultCode.Success)
                    {
                        throw new Exception($"Discreet.DB.GetTXsFromPool: database get exception: {result.resultCode}");
                    }

                    FullTransaction tx = new FullTransaction();
                    tx.Deserialize(result.value.CopyToNewArray());

                    txs[i] = tx;
                }
                else
                {
                    txs[i] = null;
                }
            }

            return txs;
        }

        public List<FullTransaction> GetTXPool()
        {
            List<FullTransaction> pool = new List<FullTransaction>();

            using var txn = Env.BeginTransaction();

            if (TXPoolBlob == null || !TXPoolBlob.IsOpened)
            {
                TXPoolBlob = txn.OpenDatabase(TX_POOL_BLOB);
            }

            LightningCursor txPoolCursor = txn.CreateCursor(TXPoolBlob);

            var resultCode = txPoolCursor.First();

            if (resultCode == MDBResultCode.NotFound) return new List<FullTransaction>();

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.GetTXPool: database get exception: {resultCode}");
            }

            var values = txPoolCursor.AsEnumerable().Select((k, i) => k.Item2).ToList();
            foreach (var value in values)
            {
                FullTransaction tx = new FullTransaction();
                tx.Deserialize(value.CopyToNewArray());

                pool.Add(tx);
            }

            return pool;
        }

        public Dictionary<long, Block> GetBlockCache()
        {
            Dictionary<long, Block> blockCache = new Dictionary<long, Block>();

            using var txn = Env.BeginTransaction();

            if (BlockCache == null || !BlockCache.IsOpened)
            {
                BlockCache = txn.OpenDatabase(BLOCK_CACHE);
            }

            LightningCursor blockCacheCursor = txn.CreateCursor(BlockCache);

            var resultCode = blockCacheCursor.First();

            if (resultCode == MDBResultCode.NotFound) return new Dictionary<long, Block>();

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.GetBlockCache: database get exception: {resultCode}");
            }

            var values = blockCacheCursor.AsEnumerable().Select((k, i) => k.Item2).ToList();
            foreach (var value in values)
            {
                byte[] bytes = value.CopyToNewArray();

                if (bytes[0] == 1 || bytes[0] == 2)
                {
                    SignedBlock block = new SignedBlock();
                    block.DeserializeFull(bytes);

                    blockCache.Add(block.Height, block);
                }
                else
                {
                    Block block = new Block();
                    block.DeserializeFull(bytes);

                    blockCache.Add(block.Height, block);
                }
            }

            return blockCache;
        }

        public void ClearBlockCache()
        {
            using var txn = Env.BeginTransaction();

            if (BlockCache == null || !BlockCache.IsOpened)
            {
                BlockCache = txn.OpenDatabase(BLOCK_CACHE);
            }

            var resultCode = BlockCache.Drop(txn);

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.GetBlockCache: database get exception: {resultCode}");
            }

            BlockCache = txn.OpenDatabase(BLOCK_CACHE, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create });

            txn.Commit();
        }

        public void AddTXToPool(FullTransaction tx)
        {
            /* the following information is checked:
             *   - if TX is already in pool
             *   - if TXPoolSpentKeys contains input key images
             *   - if SpentKeys contains input key images
             */
            using var txn = Env.BeginTransaction();

            if (TXPoolBlob == null || !TXPoolBlob.IsOpened)
            {
                TXPoolBlob = txn.OpenDatabase(TX_POOL_BLOB);
            }

            if (SpentKeys == null || !SpentKeys.IsOpened)
            {
                SpentKeys = txn.OpenDatabase(SPENT_KEYS);
            }

            if (TXPoolSpentKeys == null || !TXPoolSpentKeys.IsOpened)
            {
                TXPoolSpentKeys = txn.OpenDatabase(TX_POOL_SPENT_KEYS);
            }

            Cipher.SHA256 txhash = tx.Hash();

            if (txn.ContainsKey(TXPoolBlob, txhash.Bytes))
            {
                throw new Exception($"Discreet.DB.AddTXToPool: Transaction {txhash.ToHexShort()} already present in TXPool");
            }

            MDBResultCode resultCode;

            /* now check if spentKeys contains our key images... if so, bad news. */
            for (int i = 0; i < tx.NumPInputs; i++)
            {
                if (txn.ContainsKey(SpentKeys, tx.PInputs[i].KeyImage.bytes))
                {
                    throw new Exception($"Discreet.DB.AddTXToPool: Key image {tx.PInputs[i].KeyImage.ToHexShort()} has already been spent! (double spend)");
                }

                if (txn.ContainsKey(TXPoolSpentKeys, tx.PInputs[i].KeyImage.bytes))
                {
                    throw new Exception($"Discreet.DB.AddTXToPool: Key image {tx.PInputs[i].KeyImage.ToHexShort()} has already been spent! (double spend)");
                }

                /* now we commit them */
                resultCode = txn.Put(TXPoolSpentKeys, tx.PInputs[i].KeyImage.bytes, ZEROKEY);

                if (resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB.AddTXToPool: database update exception: {resultCode}");
                }
            }

            resultCode = txn.Put(TXPoolBlob, txhash.Bytes, tx.Serialize());

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddTXToPool: database update exception: {resultCode}");
            }

            resultCode = txn.Commit();

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddTXToPool: database update exception: {resultCode}");
            }
        }

        public bool CheckSpentKey(Cipher.Key j)
        {
            using var txn = Env.BeginTransaction();

            if (TXPoolSpentKeys == null || !TXPoolSpentKeys.IsOpened)
            {
                TXPoolSpentKeys = txn.OpenDatabase(TX_POOL_SPENT_KEYS);
            }

            if (txn.ContainsKey(TXPoolSpentKeys, j.bytes))
            {
                return false;
            }

            return true;
        }

        public bool CheckSpentKeyBlock(Cipher.Key j)
        {
            using var txn = Env.BeginTransaction();

            if (SpentKeys == null || !SpentKeys.IsOpened)
            {
                SpentKeys = txn.OpenDatabase(SPENT_KEYS);
            }

            if (txn.ContainsKey(SpentKeys, j.bytes))
            {
                return false;
            }

            return true;
        }

        public TXOutput GetOutput(uint index)
        {
            using var txn = Env.BeginTransaction();

            if (Outputs == null || !Outputs.IsOpened)
            {
                Outputs = txn.OpenDatabase(OUTPUTS);
            }

            var result = txn.Get(Outputs, Serialization.UInt32(index));

            if (result.resultCode.HasFlag(MDBResultCode.NotFound))
            {
                throw new Exception($"Discreet.DB.GetOutput: No output exists with index {index}");
            }

            TXOutput output = new TXOutput();
            output.Deserialize(result.value.CopyToNewArray());
            return output;
        }

        public TXOutput[] GetMixins(uint[] index)
        {
            using var txn = Env.BeginTransaction();

            TXOutput[] rv = new TXOutput[index.Length];

            if (Outputs == null || !Outputs.IsOpened)
            {
                Outputs = txn.OpenDatabase(OUTPUTS);
            }

            for (int i = 0; i < index.Length; i++)
            {
                byte[] key = Serialization.UInt32(index[i]);

                var result = txn.Get(Outputs, key);

                if (result.resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB.GetMixins: could not get output at index {index[i]}: {result.resultCode}");
                }

                rv[i] = new TXOutput();
                rv[i].Deserialize(result.value.CopyToNewArray());
            }

            return rv;
        }

        public (TXOutput[], int) GetMixins(uint index)
        {
            using var txn = Env.BeginTransaction();

            TXOutput[] rv = new TXOutput[64];

            if (Outputs == null || !Outputs.IsOpened)
            {
                Outputs = txn.OpenDatabase(OUTPUTS);
            }

            uint max = GetOutputIndex();

            Random rng = new Random();

            SortedSet<uint> chosen = new SortedSet<uint>();

            chosen.Add(index);

            int i = 0;

            /* the first 31 mixins are chosen uniformly from the possible set */
            for (; i < 32; )
            {
                uint rindex = (uint)rng.Next(1, (int)max);
                if (chosen.Contains(rindex)) continue;

                byte[] key = Serialization.UInt32(rindex);

                var result = txn.Get(Outputs, key);

                if (result.resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB.GetMixins: could not get output at index {rindex}: {result.resultCode}");
                }

                rv[i] = new TXOutput();
                rv[i].Deserialize(result.value.CopyToNewArray());

                rv[i].Index = rindex;

                chosen.Add(rindex);
                i++;
            }

            /* next set of mixins are chosen randomly from the triangular distribution where a = 3/4 * max and b=c=max */
            for (; i < 63; )
            {
                double uniformVariate = rng.NextDouble();
                double frac = 3.0 / 4.0 * Math.Sqrt(uniformVariate * (1.0 / 4.0) * (1.0 / 4.0));
                uint rindex = (uint)Math.Floor(frac * max);
                if (chosen.Contains(rindex) || rindex == 0) continue;

                byte[] key = Serialization.UInt32(rindex);

                var result = txn.Get(Outputs, key);

                if (result.resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB.GetMixins: could not get output at index {rindex}: {result.resultCode}");
                }

                rv[i] = new TXOutput();
                rv[i].Deserialize(result.value.CopyToNewArray());

                rv[i].Index = rindex;

                chosen.Add(rindex);
                i++;
            }

            byte[] ikey = Serialization.UInt32(index);

            var iresult = txn.Get(Outputs, ikey);

            if (iresult.resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.GetMixins: could not get output at index {index}: {iresult.resultCode}");
            }

            var OutAtIndex = rv[i] = new TXOutput();
            rv[i].Deserialize(iresult.value.CopyToNewArray());

            rv[i].Index = index;

            /* randomly shuffle */
            var rvEnumerated = rv.OrderBy(x => rng.Next(0, 64)).ToList();
            return (rvEnumerated.ToArray(), rvEnumerated.IndexOf(OutAtIndex));
        }

        public (TXOutput[], int) GetMixinsUniform(uint index)
        {
            using var txn = Env.BeginTransaction();

            TXOutput[] rv = new TXOutput[64];

            if (Outputs == null || !Outputs.IsOpened)
            {
                Outputs = txn.OpenDatabase(OUTPUTS);
            }

            uint max = GetOutputIndex();

            Random rng = new Random();

            SortedSet<uint> chosen = new SortedSet<uint>();

            chosen.Add(index);

            int i = 0;

            /* the first 31 mixins are chosen uniformly from the possible set */
            for (; i < 63;)
            {
                uint rindex = (uint)rng.Next(0, (int)max);
                if (chosen.Contains(rindex)) continue;

                byte[] key = Serialization.UInt32(rindex);

                var result = txn.Get(Outputs, key);

                if (result.resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB.GetMixinsUniform: could not get output at index {rindex}: {result.resultCode}");
                }

                rv[i] = new TXOutput();
                rv[i].Deserialize(result.value.CopyToNewArray());

                rv[i].Index = rindex;

                chosen.Add(rindex);
                i++;
            }

            byte[] ikey = Serialization.UInt32(index);

            var iresult = txn.Get(Outputs, ikey);

            if (iresult.resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.GetMixinsUniform: could not get output at index {index}: {iresult.resultCode}");
            }

            var OutAtIndex = rv[i] = new TXOutput();
            rv[i].Deserialize(iresult.value.CopyToNewArray());

            rv[i].Index = index;

            /* randomly shuffle */
            var rvEnumerated = rv.OrderBy(x => rng.Next(0, 64)).ToList();
            return (rvEnumerated.ToArray(), rvEnumerated.IndexOf(OutAtIndex));
        }

        public FullTransaction GetTransaction(ulong txid)
        {
            using var txn = Env.BeginTransaction();

            if (TXs == null || !TXs.IsOpened)
            {
                TXs = txn.OpenDatabase(TXS);
            }

            var result = txn.Get(TXs, Serialization.UInt64(txid));

            if (result.resultCode.HasFlag(MDBResultCode.NotFound))
            {
                throw new Exception($"Discreet.DB.GetTransaction: No transaction exists with index {txid}");
            }

            FullTransaction tx = new FullTransaction();
            tx.Deserialize(result.value.CopyToNewArray());
            return tx;
        }

        public FullTransaction GetTransaction(Cipher.SHA256 txhash)
        {
            using var txn = Env.BeginTransaction();

            if (TXIndices == null || !TXIndices.IsOpened)
            {
                TXIndices = txn.OpenDatabase(TX_INDICES);
            }

            if (TXs == null || !TXs.IsOpened)
            {
                TXs = txn.OpenDatabase(TXS);
            }

            var result = txn.Get(TXIndices, txhash.Bytes);

            if (result.resultCode.HasFlag(MDBResultCode.NotFound))
            {
                throw new Exception($"Discreet.DB.GetTransaction: No transaction exists with transaction hash {txhash.ToHexShort()}");
            }

            ulong txid = Serialization.GetUInt64(result.value.CopyToNewArray(), 0);

            result = txn.Get(TXs, Serialization.UInt64(txid));

            if (result.resultCode.HasFlag(MDBResultCode.NotFound))
            {
                throw new Exception($"Discreet.DB.GetTransaction: No transaction exists with index {txid}");
            }

            FullTransaction tx = new FullTransaction();
            tx.Deserialize(result.value.CopyToNewArray());
            return tx;
        }

        public Block GetBlock(long height)
        {
            using var txn = Env.BeginTransaction();

            if (Blocks == null || !Blocks.IsOpened)
            {
                Blocks = txn.OpenDatabase(BLOCKS);
            }

            var result = txn.Get(Blocks, Serialization.Int64(height));

            if (result.resultCode.HasFlag(MDBResultCode.NotFound))
            {
                throw new Exception($"Discreet.DB.GetBlock: No block exists with height {height}");
            }

            SignedBlock blk = new SignedBlock();
            blk.DeserializeFull(result.value.CopyToNewArray());
            return blk;
        }

        public Block GetBlock(Cipher.SHA256 blockHash)
        {
            using var txn = Env.BeginTransaction();

            if (BlockHeights == null || !BlockHeights.IsOpened)
            {
                BlockHeights = txn.OpenDatabase(BLOCK_HEIGHTS);
            }

            if (Blocks == null || !Blocks.IsOpened)
            {
                Blocks = txn.OpenDatabase(BLOCKS);
            }

            var result = txn.Get(BlockHeights, blockHash.Bytes);

            if (result.resultCode.HasFlag(MDBResultCode.NotFound))
            {
                throw new Exception($"Discreet.DB.GetBlock: No block exists with block hash {blockHash.ToHexShort()}");
            }

            ulong height = Serialization.GetUInt64(result.value.CopyToNewArray(), 0);

            result = txn.Get(Blocks, Serialization.UInt64(height));

            if (result.resultCode.HasFlag(MDBResultCode.NotFound))
            {
                throw new Exception($"Discreet.DB.GetBlock: No block exists with height {height}");
            }

            SignedBlock blk = new SignedBlock();
            blk.DeserializeFull(result.value.CopyToNewArray());
            return blk;
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

        public ulong GetTransactionIndex(Cipher.SHA256 txhash)
        {
            using var txn = Env.BeginTransaction();

            if (TXIndices == null || !TXIndices.IsOpened)
            {
                TXIndices = txn.OpenDatabase(TX_INDICES);
            }

            var result = txn.Get(TXIndices, txhash.Bytes);

            if (result.resultCode.HasFlag(MDBResultCode.NotFound))
            {
                throw new Exception($"Discreet.DB.GetTransactionIndex: No transaction exists with transaction hash {txhash.ToHexShort()}");
            }

            return Serialization.GetUInt64(result.value.CopyToNewArray(), 0);
        }

        public ulong GetBlockHeight(Cipher.SHA256 blockHash)
        {
            using var txn = Env.BeginTransaction();

            if (BlockHeights == null || !BlockHeights.IsOpened)
            {
                BlockHeights = txn.OpenDatabase(BLOCK_HEIGHTS);
            }

            var result = txn.Get(BlockHeights, blockHash.Bytes);

            if (result.resultCode.HasFlag(MDBResultCode.NotFound))
            {
                throw new Exception($"Discreet.DB.GetBlockHeight: No block exists with block hash {blockHash.ToHexShort()}");
            }

            return Serialization.GetUInt64(result.value.CopyToNewArray(), 0);
        }
    }
}
