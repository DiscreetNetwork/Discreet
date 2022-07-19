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

        public class MemTx
        {
            public FullTransaction Tx;
            public long Received;

            public byte[] Serialize()
            {
                byte[] data = new byte[Tx.Size() + 8];
                Coin.Serialization.CopyData(data, 0, Received);
                Tx.Serialize(data, 8);

                return data;
            }

            public void Deserialize(byte[] data)
            {
                Received = Coin.Serialization.GetInt64(data, 0);

                Tx = new FullTransaction();
                Tx.Deserialize(data, 8);
            }
        }

        private ConcurrentDictionary<Cipher.SHA256, MemTx> pool;
        private ConcurrentDictionary<Coin.Transparent.TXInput, Coin.Transparent.TXOutput> spentOutputs;
        private ConcurrentDictionary<Cipher.SHA256, List<Coin.Transparent.TXInput>> spentsTx;
        private SortedSet<Cipher.Key> spentKeys;
        private ConcurrentDictionary<Cipher.SHA256, List<Cipher.Key>> updateSpentKeys;

        public TXPool()
        {
            pool = new();
            spentOutputs = new();
            spentsTx = new();
            spentKeys = new(new Cipher.KeyComparer());
            updateSpentKeys = new();

            //DB.DisDB db = DB.DisDB.GetDB();

            //var _pool = db.GetTXPool();

            // TODO: add back in after sync code is complete
            /*foreach (var tx in _pool)
            {
                pool[tx.Tx.Hash()] = tx;
            }

            foreach (var tx in _pool)
            {
                if (tx.Tx.TInputs != null && tx.Tx.TInputs.Length > 0)
                {
                    List<Coin.Transparent.TXInput> stxs = new List<Coin.Transparent.TXInput>();
                    
                    foreach (var txi in tx.Tx.TInputs)
                    {
                        spentOutputs[txi] = db.GetPubOutput(txi);
                        stxs.Add(txi);
                    }

                    spentsTx[tx.Tx.TxID] = stxs;
                }

                if (tx.Tx.PInputs != null && tx.Tx.PInputs.Length > 0)
                {
                    List<Cipher.Key> ks = new List<Cipher.Key>();

                    foreach (var ptxi in tx.Tx.PInputs)
                    {
                        spentKeys.Add(ptxi.KeyImage);
                        ks.Add(ptxi.KeyImage);
                    }

                    updateSpentKeys[tx.Tx.TxID] = ks;
                }
            }*/
        }

        public Exception ProcessIncoming(FullTransaction tx)
        {
            /* verify transaction */
            var err = tx.Verify();

            if (err != null)
            {
                return err;
            }

            /* since it is verified, create a new memtx */
            var memtx = new MemTx { Tx = tx, Received = DateTime.UtcNow.Ticks };

            /* try adding to database */
            DB.DisDB db = DB.DisDB.GetDB();
            try
            {
                lock (DB.DisDB.DBLock)
                {
                    db.AddTXToPool(memtx);
                }
            }
            catch (Exception e)
            {
                return new DatabaseException("Discreet.Daemon.TXPool.ProcessIncoming", e.Message);
            }

            /* no errors, so add TX to pool */
            pool[tx.Hash()] = memtx;

            /* update the stuff */
            if (tx.TInputs != null && tx.TInputs.Length > 0)
            {
                List<Coin.Transparent.TXInput> stxs = new List<Coin.Transparent.TXInput>();

                foreach (var txi in tx.TInputs)
                {
                    spentOutputs[txi] = db.GetPubOutput(txi);
                    stxs.Add(txi);
                }

                spentsTx[tx.TxID] = stxs;
            }

            if (tx.PInputs != null && tx.PInputs.Length > 0)
            {
                List<Cipher.Key> ks = new List<Cipher.Key>();

                foreach (var ptxi in tx.PInputs)
                {
                    spentKeys.Add(ptxi.KeyImage);
                    ks.Add(ptxi.KeyImage);
                }

                updateSpentKeys[tx.TxID] = ks;
            }

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

        public bool ContainsSpentKey(Cipher.Key k)
        {
            return spentKeys.Contains(k);
        }

        public List<FullTransaction> SelectAndRemove(int maxBytes)
        {
            var searchTxs = pool.Values.OrderBy(x => x.Received).ToList();
            var txs = new List<FullTransaction>();
            // TODO: change once block headers are fixed size
            uint _sizeTotal = 137 + 96;

            while (_sizeTotal < maxBytes)
            {
                var tx = searchTxs.FirstOrDefault();

                if (tx != null && tx.Tx.Size() + _sizeTotal < maxBytes)
                {
                    txs.Add(tx.Tx);
                    searchTxs.Remove(tx);

                    _sizeTotal += tx.Tx.Size();
                }
                else
                {
                    break;
                }
            }

            return txs;
        }

        /**
         * Grabs transactions from the pool and packs them into a block.
         */
        public List<FullTransaction> GetTransactionsForBlock()
        {
            return SelectAndRemove(1048576);
        }

        public List<FullTransaction> GetTransactions()
        {
            return pool.Values.Select(x => x.Tx).ToList();
        }

        public void UpdatePool(IEnumerable<Cipher.SHA256> txs)
        {
            txs.ToList().ForEach(x => pool.Remove(x, out _));

            //DB.DisDB.GetDB().UpdateTXPool(txs);
        }

        public bool Contains(Cipher.SHA256 txhash)
        {
            return pool.ContainsKey(txhash);
            //DB.DisDB db = DB.DisDB.GetDB();
            //return db.TXPoolContains(txhash);
        }

        public bool ContainsSpent(Coin.Transparent.TXInput txoidx)
        {
            return spentOutputs.ContainsKey(txoidx);
        }

        public void UpdatePool(IEnumerable<FullTransaction> blockTxs)
        {
            var hashes = blockTxs.Select(x => x.Hash());
            hashes.ToList().ForEach(x => pool.Remove(x, out _));

            DB.DisDB.GetDB().UpdateTXPool(hashes);

            // remove spent trackers based on inclusion in block
            foreach (var hash in hashes)
            {
                if (spentsTx.ContainsKey(hash))
                {
                    foreach (var txohash in spentsTx[hash])
                    {
                        spentOutputs.Remove(txohash, out _);
                    }
                    spentsTx.Remove(hash, out _);
                }

                if (updateSpentKeys.ContainsKey(hash))
                {
                    foreach (var kv in updateSpentKeys[hash])
                    {
                        spentKeys.Remove(kv);
                    }
                    updateSpentKeys.Remove(hash, out _);
                }
            }
        }

        public FullTransaction GetTransaction(Cipher.SHA256 txhash)
        {
            bool res = pool.TryGetValue(txhash, out MemTx memtx);

            if (!res)
            {
                throw new Exception($"TXPool.GetTransaction: could not find tx with hash {txhash.ToHexShort()}");
            }

            return memtx.Tx;
        }
    }
}
