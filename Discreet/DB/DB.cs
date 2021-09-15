using System;
using System.Collections.Generic;
using System.Text;
using LightningDB;
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
     */
    public class DB
    {
        //WIP

        /* table keys */
        public static string SPENT_KEYS = "spent_keys";
        public static string TX_POOL_META = "tx_pool_meta";
        public static string TX_POOL_BLOB = "tx_pool_blob";
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
        private LightningDatabase Outputs;
        private LightningDatabase TXIndices;
        private LightningDatabase TXs;
        private LightningDatabase BlockInfo;
        private LightningDatabase BlockHeights;
        private LightningDatabase Blocks;

        private string Folder;

        private bool IsOpen;

        public void Open(string filename)
        {
            if (IsOpen) throw new Exception("Discreet.DB: Database is already open");

            if (File.Exists(filename)) throw new Exception("Discreet.DB: Open() expects a valid directory path, not a file");

            //if (Directory.CreateDirectory(filename)) 
        }

        public DB()
        {

        }

        public void CheckOpen()
        {
            /*if (!Database.IsOpened)
            {
                throw new Exception("Discreet.DB: Database is not open!");
            }*/
        }

        public void AddBlock(Block blk)
        {
            CheckOpen();


        }
    }
}
