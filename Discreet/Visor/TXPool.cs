﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;

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

        private List<Transaction> pool;

        public TXPool()
        {
            DB.DB db = DB.DB.GetDB();

            pool = db.GetTXPool();
        }

        public Exception ProcessIncoming(byte[] txBytes)
        {
            /* try to decode the transaction, and catch any error thrown */
            Transaction tx = new Transaction();
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
    }
}