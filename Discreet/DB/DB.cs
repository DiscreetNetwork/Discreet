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
                    string homePath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
                    db = new DB(Path.Combine(homePath, ".discreet"));
                }
            }
        }

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

        public string Folder
        {
            get { return folder; }
        }

        private string folder;

        private U64 indexer_tx = new U64(0);
        private U32 indexer_output = new U32(0);

        private U64 height = new U64(0);

        private U64 previouslySeenTimestamp = new U64((ulong)DateTime.Now.Ticks);

        private long mapsize = 0;

        private U32 indexer_owned_outputs = new U32(0);

        public long Mapsize {
            get { return mapsize; }
        }


        public void Open(string filename, long _mapsize)
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

        public void Open(string filename)
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
                Env.MapSize = 1024 * 1024 * 1024;
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

        public DB(string path)
        {
            Open(path);

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

            /* populate our indexers */
            Meta = txn.OpenDatabase(META, config);

            if (!txn.ContainsKey(Meta, Encoding.ASCII.GetBytes("meta")))
            {
                /* completely empty and has just been created */
                txn.Put(Meta, Encoding.ASCII.GetBytes("meta"), ZEROKEY);
                txn.Put(Meta, Encoding.ASCII.GetBytes("indexer_tx"), Serialization.UInt64(indexer_tx.Value));
                txn.Put(Meta, Encoding.ASCII.GetBytes("indexer_output"), Serialization.UInt32(indexer_output.Value));
                txn.Put(Meta, Encoding.ASCII.GetBytes("height"), Serialization.UInt64(height.Value));
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

                height.Value = Serialization.GetUInt64(result.value.CopyToNewArray(), 0);

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

            var result = txn.Get(Blocks, Serialization.UInt64(blk.Height));

            if (result.resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddBlock: Block {blk.BlockHash.ToHexShort()} (height {blk.Height}) already in database!");
            }

            result = txn.Get(BlockHeights, blk.PreviousBlock.Bytes);

            if (result.resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddBlock: Previous block hash {blk.PreviousBlock.ToHexShort()} for block {blk.BlockHash.ToHexShort()} (height {blk.Height}) not found");
            }

            ulong prevBlockHeight = Serialization.GetUInt64(result.value.CopyToNewArray(), 0);
            if (prevBlockHeight != height.Value)
            {
                throw new Exception($"Discreet.DB.AddBlock: Previous block hash {blk.PreviousBlock.ToHexShort()} for block {blk.BlockHash.ToHexShort()} (height {blk.Height}) not previous one in sequence (at height {prevBlockHeight})");
            }

            lock (previouslySeenTimestamp) {
                if (previouslySeenTimestamp.Value >= blk.Timestamp)
                {
                    throw new Exception($"Discreet.DB.AddBlock: Block timestamp {blk.Timestamp} not occurring after previously seen timestamp {previouslySeenTimestamp.Value}");
                }
                previouslySeenTimestamp.Value = blk.Timestamp;
            }

            lock (height)
            {
                if (blk.Height != height.Value + 1)
                {
                    throw new Exception($"Discreeet.DB.AddBlock: block height {blk.Height} not in sequence!");
                }

                height.Value++;
            }

            var resCode = txn.Put(BlockHeights, blk.BlockHash.Bytes, Serialization.UInt64(blk.Height));

            if (resCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddBlock: database update exception: {resCode}");
            }

            resCode = txn.Put(Blocks, Serialization.UInt64(blk.Height), blk.Marshal());

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
            for (int i = 0; i < blk.NumTXs; i++)
            {
                if (!txn.ContainsKey(TXPoolBlob, blk.Transactions[i].Bytes))
                {
                    throw new Exception($"Discreet.DB.AddBlock: Transaction in block {blk.BlockHash.ToHexShort()} ({blk.Transactions[i].ToHexShort()}) not in transaction pool");
                }
            }

            /* We currently sort by transaction hash to order transactions. */
            Cipher.SHA256[] sortedTransactions = new Cipher.SHA256[blk.NumTXs];
            for (int i = 0; i < blk.NumTXs; i++)
            {
                sortedTransactions[i] = new Cipher.SHA256(blk.Transactions[i].GetBytes(), false);
            }

            /* Ugly ass selection sort lmao */
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
            }

            if (TXIndices == null || !TXIndices.IsOpened)
            {
                TXIndices = txn.OpenDatabase(TX_INDICES);
            }

            if (TXs == null || !TXs.IsOpened)
            {
                TXs = txn.OpenDatabase(TXS);
            }

            /* Now we add transactions from transaction pool */
            for (int i = 0; i < blk.NumTXs; i++)
            {
                AddTransactionFromPool(sortedTransactions[i], txn);
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
                resCode = txn.Put(Meta, Encoding.ASCII.GetBytes("indexer_output"), Serialization.UInt64(indexer_output.Value));

                if (resCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB: Error while updating output indexer: {resCode}");
                }
            }

            lock (height)
            {
                resCode = txn.Put(Meta, Encoding.ASCII.GetBytes("height"), Serialization.UInt64(height.Value));

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
            Transaction tx = new Transaction();
            tx.Unmarshal(txraw);

            uint[] outputIndices = new uint[tx.NumOutputs];

            for (int i = 0; i < tx.NumOutputs; i++)
            {
                outputIndices[i] = AddOutput(tx.Outputs[i], txn);
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

            for (int i = 0; i < tx.NumInputs; i++)
            {
                resultCode = txn.Put(SpentKeys, tx.Inputs[i].KeyImage.bytes, ZEROKEY);

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

            var resultCode = txn.Put(Outputs, Serialization.UInt32(outputIndex), output.Marshal());

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

        public uint GetTXOutputIndex(LightningTransaction txn, Transaction tx, int i)
        {
            if (OutputIndices == null || !OutputIndices.IsOpened)
            {
                OutputIndices = txn.OpenDatabase(OUTPUT_INDICES);
            }

            var result = txn.Get(OutputIndices, tx.Hash().Bytes);

            if (result.resultCode == MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.GetTXOutputIndex: database get exception: {result.resultCode}");
            }

            uint[] outputIndices = Serialization.GetUInt32Array(result.value.CopyToNewArray());

            return outputIndices[i];
        }

        public int AddWalletOutput(Transaction tx, int i)
        {
            var txn = Env.BeginTransaction();

            uint index = GetTXOutputIndex(txn, tx, i);

            Wallets.UTXO utxo = new Wallets.UTXO(index, tx.Outputs[i], tx, i);

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

            var resCode = txn.Put(OwnedOutputs, Serialization.Int32(outputIndex), utxo.Marshal());

            if (resCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddWalletOutput: database update exception: {resCode}");
            }

            lock (indexer_owned_outputs)
            {
                resCode = txn.Put(Meta, Encoding.ASCII.GetBytes("indexer_owned_outputs"), Serialization.UInt64(indexer_owned_outputs.Value));

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

            return outputIndex;
        }

        public Wallets.UTXO GetWalletOutput(int index)
        {
            var txn = Env.BeginTransaction();

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
            utxo.Unmarshal(result.value.CopyToNewArray());

            return utxo;
        }

        public void AddTXToPool(Transaction tx)
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

            if (SpentKeys == null || !SpentKeys.IsOpened)
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
            for (int i = 0; i < tx.NumInputs; i++)
            {
                if (txn.ContainsKey(SpentKeys, tx.Inputs[i].KeyImage.bytes))
                {
                    throw new Exception($"Discreet.DB.AddTXToPool: Key image {tx.Inputs[i].KeyImage.ToHexShort()} has already been spent! (double spend)");
                }

                if (txn.ContainsKey(TXPoolSpentKeys, tx.Inputs[i].KeyImage.bytes))
                {
                    throw new Exception($"Discreet.DB.AddTXToPool: Key image {tx.Inputs[i].KeyImage.ToHexShort()} has already been spent! (double spend)");
                }

                /* now we commit them */
                resultCode = txn.Put(TXPoolSpentKeys, tx.Inputs[i].KeyImage.bytes, ZEROKEY);

                if (resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB.AddTXToPool: database update exception: {resultCode}");
                }
            }

            resultCode = txn.Put(TXPoolBlob, txhash.Bytes, tx.Marshal());

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
            output.Unmarshal(result.value.CopyToNewArray());
            return output;
        }

        public TXOutput[] GetMixins(uint[] index)
        {
            var txn = Env.BeginTransaction();

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
                rv[i].Unmarshal(result.value.CopyToNewArray());
            }

            return rv;
        }

        public (TXOutput[], int) GetMixins(uint index)
        {
            var txn = Env.BeginTransaction();

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
                uint rindex = (uint)rng.Next(0, (int)max);
                if (chosen.Contains(rindex)) continue;

                byte[] key = Serialization.UInt32(rindex);

                var result = txn.Get(Outputs, key);

                if (result.resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB.GetMixins: could not get output at index {rindex}: {result.resultCode}");
                }

                rv[i] = new TXOutput();
                rv[i].Unmarshal(result.value.CopyToNewArray());

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
                if (chosen.Contains(rindex)) continue;

                byte[] key = Serialization.UInt32(rindex);

                var result = txn.Get(Outputs, key);

                if (result.resultCode != MDBResultCode.Success)
                {
                    throw new Exception($"Discreet.DB.GetMixins: could not get output at index {rindex}: {result.resultCode}");
                }

                rv[i] = new TXOutput();
                rv[i].Unmarshal(result.value.CopyToNewArray());

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
            rv[i].Unmarshal(iresult.value.CopyToNewArray());

            rv[i].Index = index;

            /* randomly shuffle */
            var rvEnumerated = rv.OrderBy(x => rng.Next(0, 64)).ToList();
            return (rvEnumerated.ToArray(), rvEnumerated.IndexOf(OutAtIndex));
        }

        public (TXOutput[], int) GetMixinsUniform(uint index)
        {
            var txn = Env.BeginTransaction();

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
                rv[i].Unmarshal(result.value.CopyToNewArray());

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
            rv[i].Unmarshal(iresult.value.CopyToNewArray());

            rv[i].Index = index;

            /* randomly shuffle */
            var rvEnumerated = rv.OrderBy(x => rng.Next(0, 64)).ToList();
            return (rvEnumerated.ToArray(), rvEnumerated.IndexOf(OutAtIndex));
        }

        public Transaction GetTransaction(ulong txid)
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

            Transaction tx = new Transaction();
            tx.Unmarshal(result.value.CopyToNewArray());
            return tx;
        }

        public Transaction GetTransaction(Cipher.SHA256 txhash)
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

            Transaction tx = new Transaction();
            tx.Unmarshal(result.value.CopyToNewArray());
            return tx;
        }

        public Block GetBlock(ulong height)
        {
            using var txn = Env.BeginTransaction();

            if (Blocks == null || !Blocks.IsOpened)
            {
                Blocks = txn.OpenDatabase(BLOCKS);
            }

            var result = txn.Get(Blocks, Serialization.UInt64(height));

            if (result.resultCode.HasFlag(MDBResultCode.NotFound))
            {
                throw new Exception($"Discreet.DB.GetBlock: No block exists with height {height}");
            }

            Block blk = new Block();
            blk.Unmarshal(result.value.CopyToNewArray());
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

            Block blk = new Block();
            blk.Unmarshal(result.value.CopyToNewArray());
            return blk;
        }

        public uint GetOutputIndex()
        {
            lock (indexer_output)
            {
                return indexer_output.Value;
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
