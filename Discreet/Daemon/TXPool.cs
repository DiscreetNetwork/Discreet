using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;
using Discreet.Common.Exceptions;

namespace Discreet.Daemon
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

        private ConcurrentDictionary<Cipher.SHA256, FullTransaction> pool;

        public TXPool()
        {
            pool = new();

            DB.DisDB db = DB.DisDB.GetDB();

            var _pool = db.GetTXPool();

            foreach (var tx in _pool)
            {
                pool[tx.Hash()] = tx;
            }
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
            DB.DisDB db = DB.DisDB.GetDB();
            try
            {
                lock (DB.DisDB.DBLock)
                {
                    db.AddTXToPool(tx);
                }
            }
            catch (Exception e)
            {
                return new DatabaseException("Discreet.Visor.TXPool.ProcessIncoming", e.Message);
            }

            /* no errors, so add TX to pool */
            pool[tx.Hash()] = tx;

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
            return pool.Values.ToList();
        }

        public List<FullTransaction> GetTransactions()
        {
            return pool.Values.ToList();
        }

        public void UpdatePool(IEnumerable<Cipher.SHA256> txs)
        {
            txs.ToList().ForEach(x => pool.Remove(x, out _));

            DB.DisDB.GetDB().UpdateTXPool(txs);
        }

        public bool Contains(Cipher.SHA256 txhash)
        {
            DB.DisDB db = DB.DisDB.GetDB();

            return db.TXPoolContains(txhash);
        }

        public void UpdatePool(IEnumerable<FullTransaction> blockTxs)
        {
            var hashes = blockTxs.Select(x => x.Hash());
            hashes.ToList().ForEach(x => pool.Remove(x, out _));

            DB.DisDB.GetDB().UpdateTXPool(hashes);
        }
    }
}
