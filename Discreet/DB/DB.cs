﻿using System;
using System.Collections.Generic;
using System.Text;
using LightningDB;
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
        //WIP

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

        /* zero key */
        public static byte[] ZEROKEY = new byte[8];

        /* DB txn cursors */
        private LightningCursor CursorSpentKeys;
        private LightningCursor CursorTXPoolMeta;
        private LightningCursor CursorTXPoolBlob;
        private LightningCursor CursorTXPoolSpentKeys;
        private LightningCursor CursorOutputs;
        private LightningCursor CursorTXIndices;
        private LightningCursor CursorTXs;
        private LightningCursor CursorBlockInfo;
        private LightningCursor CursorBlockHeights;
        private LightningCursor CursorBlocks;

        /* Environment */
        private LightningEnvironment Environment;

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

        private string Folder;

        private U64 indexer_tx = new U64(0);
        private U32 indexer_output = new U32(0);

        private U64 height = new U64(0);

        private U64 previouslySeenTimestamp = new U64((ulong)DateTime.Now.Ticks);


        public void Open(string filename)
        {
            lock (Environment)
            {
                if (Environment != null && Environment.IsOpened) return;

                if (File.Exists(filename)) throw new Exception("Discreet.DB: Open() expects a valid directory path, not a file");

                if (!Directory.Exists(filename))
                {
                    Directory.CreateDirectory(filename);
                }

                Environment = new LightningEnvironment(filename);
                Environment.Open();
            }
        }

        public DB(string path)
        {
            Open(path);

            using var txn = Environment.BeginTransaction();
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

            txn.Commit();

            /*
            CursorSpentKeys = txn.CreateCursor(SpentKeys);
            CursorTXPoolMeta = txn.CreateCursor(TXPoolMeta);
            CursorTXPoolBlob = txn.CreateCursor(TXPoolBlob);
            CursorOutputs = txn.CreateCursor(Outputs);
            CursorTXIndices = txn.CreateCursor(TXIndices);
            CursorTXs = txn.CreateCursor(TXs);
            CursorBlockInfo = txn.CreateCursor(BlockInfo);
            CursorBlockHeights = txn.CreateCursor(BlockHeights);
            CursorBlocks = txn.CreateCursor(Blocks);
            */
        }

        public void AddBlock(Block blk)
        {
            using var txn = Environment.BeginTransaction();

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

            if(TXIndices == null || !TXIndices.IsOpened)
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

            for (int i = 0; i < tx.NumOutputs; i++)
            {
                AddOutput(tx.Outputs[i], txn);
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

        private void AddOutput(TXOutput output, LightningTransaction txn)
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

            /*resultCode = txn.Commit();

            if (resultCode != MDBResultCode.Success)
            {
                throw new Exception($"Discreet.DB.AddOutput: database update exception: {resultCode}");
            }*/
        }

        public void AddTXToPool(Transaction tx)
        {
            /* the following information is checked:
             *   - if TX is already in pool
             *   - if TXPoolSpentKeys contains input key images
             *   - if SpentKeys contains input key images
             */
            using var txn = Environment.BeginTransaction();

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
            using var txn = Environment.BeginTransaction();

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

        public Transaction GetTransaction(ulong txid)
        {
            using var txn = Environment.BeginTransaction();

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
            using var txn = Environment.BeginTransaction();

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
            using var txn = Environment.BeginTransaction();

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
            using var txn = Environment.BeginTransaction();

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
            using var txn = Environment.BeginTransaction();

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
            using var txn = Environment.BeginTransaction();

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
