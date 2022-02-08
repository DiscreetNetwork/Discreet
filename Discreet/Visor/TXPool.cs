using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;
using Discreet.Common.Exceptions;

namespace Discreet.Visor
{
    public class TXPool
    {
        private static TXPool txpool;

        private static object txpool_lock = new object();

        public static TXPool GetTXPool()
        {
            lock (txpool_lock)
            {
                if (txpool == null) Initialize();

                return txpool;
            }
        }

        public static void Initialize()
        {
            lock (txpool_lock)
            {
                if (txpool == null)
                {
                    txpool = new TXPool();
                }
            }
        }

        private List<FullTransaction> pool;

        public TXPool()
        {
            DB.DB db = DB.DB.GetDB();

            pool = db.GetTXPool();
        }

        public Exception ProcessIncoming(byte[] txBytes)
        {
            /* try to decode the transaction, and catch any error thrown */
            FullTransaction tx = new FullTransaction();
            try
            {
                tx.Unmarshal(txBytes);
            }
            catch (Exception e)
            {
                return new DecodeException("Discreet.Visor.TXPool.ProcessIncoming", "Transaction", e.Message);
            }

            /* verify transaction */
            var err = tx.Verify();

            if (err != null)
            {
                return err;
            }

            /* try adding to database */
            DB.DB db = DB.DB.GetDB();
            try
            {
                db.AddTXToPool(tx);
            }
            catch (Exception e)
            {
                return new DatabaseException("Discreet.Visor.TXPool.ProcessIncoming", e.Message);
            }

            /* no errors, so add TX to pool */
            pool.Add(tx);

            return null;
        }

        public Exception ProcessIncoming(FullTransaction tx)
        {
            /* verify transaction */
            var err = tx.Verify();

            if (err != null)
            {
                return err;
            }

            /* try adding to database */
            DB.DB db = DB.DB.GetDB();
            try
            {
                lock (DB.DB.DBLock)
                {
                    db.AddTXToPool(tx);
                }
            }
            catch (Exception e)
            {
                return new DatabaseException("Discreet.Visor.TXPool.ProcessIncoming", e.Message);
            }

            /* no errors, so add TX to pool */
            pool.Add(tx);

            return null;
        }

        public Exception ProcessIncoming(MixedTransaction tx)
        {
            return ProcessIncoming(tx.ToFull());
        }

        public Exception ProcessIncoming(Transaction tx)
        {
            return ProcessIncoming(tx.ToFull());
        }

        public Exception ProcessIncoming(Coin.Transparent.Transaction tx)
        {
            return ProcessIncoming(tx.ToFull());
        }

        /**
         * Grabs transactions from the pool and packs them into a block.
         * Currently just returns the entire TXpool.
         */
        public List<FullTransaction> GetTransactionsForBlock()
        {
            return pool;
        }

        public bool Contains(Cipher.SHA256 txhash)
        {
            DB.DB db = DB.DB.GetDB();

            return db.TXPoolContains(txhash);
        }

        /* */
        public void UpdatePool(List<FullTransaction> blockTxs)
        {
            foreach (FullTransaction tx in blockTxs)
            {
                if(!pool.Remove(tx))
                {
                    Logger.Log($"Discreet.Visor.TXPool.UpdatePool: transaction with hash {tx.Hash()} not present in txpool");
                }
            }
        }
    }
}
