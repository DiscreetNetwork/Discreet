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
        private ConcurrentDictionary<Coin.Transparent.TXInput, Coin.Transparent.TXOutput> newOutputs;
        private ConcurrentDictionary<Cipher.SHA256, List<Coin.Transparent.TXInput>> updateNewOutputs;

        private ConcurrentDictionary<Cipher.SHA256, FullTransaction> orphanTxs;

        private DB.DataView view;

        public TXPool()
        {
            pool = new();
            spentOutputs = new();
            spentsTx = new();
            spentKeys = new(new Cipher.KeyComparer());
            updateSpentKeys = new();
            orphanTxs = new();

            view = DB.DataView.GetView();

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
            //DB.DisDB db = DB.DisDB.GetDB();
            try
            {
                //lock (DB.DisDB.DBLock)
                //{
                //    db.AddTXToPool(memtx);
                //}
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
                    spentOutputs[txi] = DB.DataView.GetView().GetPubOutput(txi);
                    stxs.Add(txi);
                }

                spentsTx[tx.TxID] = stxs;
            }

            if (tx.TOutputs != null && tx.TOutputs.Length > 0)
            {
                List<Coin.Transparent.TXInput> newOuts = new List<Coin.Transparent.TXInput>();

                for (int i = 0; i < tx.TOutputs.Length; i++)
                {
                    var newOut = new Coin.Transparent.TXInput(tx.TxID, (byte)i);
                    newOuts.Add(newOut);
                    newOutputs[newOut] = tx.TOutputs[i];
                }

                updateNewOutputs[tx.TxID] = newOuts;
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

        public void AddToPool(MemTx tx)
        {
            pool[tx.Tx.Hash()] = tx;

            /* update the stuff */
            if (tx.Tx.TInputs != null && tx.Tx.TInputs.Length > 0)
            {
                List<Coin.Transparent.TXInput> stxs = new List<Coin.Transparent.TXInput>();

                foreach (var txi in tx.Tx.TInputs)
                {
                    if (newOutputs.TryGetValue(txi, out var txo))
                    {
                        newOutputs.Remove(txi, out _);
                        spentOutputs[txi] = txo;
                    }
                    else
                    {
                        spentOutputs[txi] = view.GetPubOutput(txi);
                    }
                    stxs.Add(txi);
                }

                spentsTx[tx.Tx.TxID] = stxs;
            }

            if (tx.Tx.TOutputs != null && tx.Tx.TOutputs.Length > 0)
            {
                List<Coin.Transparent.TXInput> newOuts = new List<Coin.Transparent.TXInput>();

                for (int i = 0; i < tx.Tx.TOutputs.Length; i++)
                {
                    var newOut = new Coin.Transparent.TXInput(tx.Tx.TxID, (byte)i);
                    newOuts.Add(newOut);
                    newOutputs[newOut] = tx.Tx.TOutputs[i];
                }

                updateNewOutputs[tx.Tx.TxID] = newOuts;
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

            //DB.DisDB.GetDB().UpdateTXPool(hashes);

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

                if (updateNewOutputs.ContainsKey(hash))
                {
                    foreach (var noutdat in updateNewOutputs[hash])
                    {
                        newOutputs.Remove(noutdat, out _);
                    }
                    updateNewOutputs.Remove(hash, out _);
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

        public Exception CheckTx(FullTransaction tx)
        {
            var npin = (tx.PInputs == null) ? 0 : tx.PInputs.Length;
            var npout = (tx.POutputs == null) ? 0 : tx.POutputs.Length;
            var ntin = (tx.TInputs == null) ? 0 : tx.TInputs.Length;
            var ntout = (tx.TOutputs == null) ? 0 : tx.TOutputs.Length;

            var npsg = (tx.PSignatures == null) ? 0 : tx.PSignatures.Length;
            var ntsg = (tx.TSignatures == null) ? 0 : tx.TSignatures.Length;

            /* precheck tx */
            var precheckExc = tx.Precheck();
            if (precheckExc != null) return precheckExc;

            /* reject if already present in pool or main branch */
            if (pool.ContainsKey(tx.TxID)) return new VerifyException("FullTransaction", $"Transaction {tx.TxID.ToHexShort()} already present in pool");
            if (view.ContainsTransaction(tx.TxID)) return new VerifyException("FullTransaction", $"Transaction {tx.TxID.ToHexShort()} already present in main branch");

            /* transparent checks */
            Coin.Transparent.TXOutput[] tinVals = new Coin.Transparent.TXOutput[ntin];
            if (ntin > 0)
            {
                /* check for spend in pool */
                HashSet<Coin.Transparent.TXInput> _in = new HashSet<Coin.Transparent.TXInput>(new Coin.Transparent.TXInputEqualityComparer());
                for (int i = 0; i < ntin; i++)
                {
                    _in.Add(tx.TInputs[i]);

                    if (spentOutputs.ContainsKey(tx.TInputs[i])) return new VerifyException("FullTransaction", $"Transparent input at index {i} was spent in a previous transaction currently in the mempool");
                }
            }

            /* more transparent checks */
            if (ntin > 0)
            {
                /* check orphan data */
                HashSet<Cipher.SHA256> missingTxs = new HashSet<Cipher.SHA256>();
                for (int i = 0; i < ntin; i++)
                {
                    if (!Contains(tx.TInputs[i].TxSrc) && !view.ContainsTransaction(tx.TInputs[i].TxSrc)) missingTxs.Add(tx.TInputs[i].TxSrc);
                }

                /* add orphan data */
                if (missingTxs.Count > 0)
                {
                    foreach (var h in missingTxs)
                    {
                        orphanTxs[h] = tx;
                    }

                    return new VerifyException("FullTransaction", $"Transaction is missing outputs; orphaned");
                }

                /* grab matching outputs for inputs */
                for (int i = 0; i < ntin; i++)
                {
                    bool res = newOutputs.TryGetValue(tx.TInputs[i], out tinVals[i]);
                    if (!res)
                    {
                        try
                        {
                            tinVals[i] = view.GetPubOutput(tx.TInputs[i]);
                        }
                        catch
                        {
                            return new VerifyException("FullTransaction", $"Transaction transparent input at index {i} has no matching output (double spend or fake)");
                        }
                    }
                }

                /* check address info */
                for (int i = 0; i < ntin; i++)
                {
                    var aexc = tinVals[i].Address.Verify();
                    if (aexc != null) return aexc;

                    if (!tinVals[i].Address.CheckAddressBytes(tx.TSignatures[i].y))
                    {
                        return new VerifyException("FullTransaction", $"Transparent input at index {i}'s address ({tinVals[i].Address}) does not match public key in signature ({tx.TSignatures[i].y.ToHexShort()})");
                    }
                }
            }
            
            /* private checks */
            var npsout = (tx.PseudoOutputs == null) ? 0 : tx.PseudoOutputs.Length;
            TXOutput[][] mixins = new TXOutput[npin][];
            if (npin > 0)
            {
                var uncheckedTags = new List<Cipher.Key>(tx.PInputs.Select(x => x.KeyImage));

                for (int i = 0; i < npin; i++)
                {
                    /* verify no duplicate spends in pool or main branch */
                    if (!view.CheckSpentKey(tx.PInputs[i].KeyImage) || !ContainsSpentKey(tx.PInputs[i].KeyImage)) return new VerifyException("FullTransaction", $"Private input's key image ({tx.PInputs[i].KeyImage.ToHexShort()}) already spent");

                    /* verify existence of all mixins */
                    try
                    {
                        mixins[i] = view.GetMixins(tx.PInputs[i].Offsets);
                    }
                    catch
                    {
                        return new VerifyException("FullTransaction", $"Private input at index {i} has invalid or missing mixin data");
                    }
                }
            }

            /* calculate tinAmt */
            ulong tinAmt = 0;
            foreach (Coin.Transparent.TXOutput output in tinVals)
            {
                try
                {
                    tinAmt = checked(tinAmt + output.Amount);
                }
                catch (OverflowException)
                {
                    return new VerifyException("FullTransaction", $"Transparent input sum resulted in overflow");
                }
            }

            /* calculate toutAmt + Fee */
            ulong toutAmt = 0;
            foreach (Coin.Transparent.TXOutput output in tx.TOutputs)
            {
                try
                {
                    toutAmt = checked(toutAmt + output.Amount);
                }
                catch (OverflowException)
                {
                    return new VerifyException("FullTransaction", $"Transparent output sum resulted in overflow");
                }
            }
            try
            {
                toutAmt = checked(tx.Fee + toutAmt);
            }
            catch (OverflowException)
            {
                return new VerifyException("FullTransaction", $"Transaction fee + output sum resulted in overflow");
            }

            Cipher.Key tmp = new(new byte[32]);

            /* calculate pinAmt */
            Cipher.Key pinAmt = new(new byte[32]);
            for (int i = 0; i < npsout; i++)
            {
                Cipher.KeyOps.AddKeys(ref tmp, ref pinAmt, ref tx.PseudoOutputs[i]);
                Array.Copy(tmp.bytes, pinAmt.bytes, 32);
            }

            /* calculate poutAmt */
            Cipher.Key poutAmt = new(new byte[32]);
            for (int i = 0; i < npout; i++)
            {
                Cipher.KeyOps.AddKeys(ref tmp, ref poutAmt, ref tx.POutputs[i].Commitment);
                Array.Copy(tmp.bytes, poutAmt.bytes, 32);
            }

            /* commit + sum tinAmt to pinAmt */
            Cipher.Key _Z = Cipher.Key.Copy(Cipher.Key.Z);
            Cipher.Key inAmt = new(new byte[32]);
            Cipher.KeyOps.GenCommitment(ref tmp, ref _Z, tinAmt);
            Cipher.KeyOps.AddKeys(ref inAmt, ref pinAmt, ref tmp);

            /* commit + sum toutAmt to poutAmt */
            Cipher.Key outAmt = new(new byte[32]);
            Cipher.KeyOps.GenCommitment(ref tmp, ref _Z, toutAmt);
            Cipher.KeyOps.AddKeys(ref outAmt, ref poutAmt, ref tmp);

            /* verify sumIn = sumOut */
            if (!inAmt.Equals(outAmt)) return new VerifyException("FullTransaction", $"Transaction does not balance");

            /* verify range proof */
            VerifyException bpexc;
            if (tx.RangeProof != null)
            {
                bpexc = tx.RangeProof.Verify(tx);
                if (bpexc != null) return bpexc;
            }
            else if (tx.RangeProofPlus != null)
            {
                bpexc = tx.RangeProofPlus.Verify(tx);
                if (bpexc != null) return bpexc;
            }

            /* verify transparent signatures */
            for (int i = 0; i < ntin; i++)
            {
                if (tx.TSignatures[i].IsNull()) return new VerifyException("FullTransaction", $"Unsigned transparent input at index {i}");

                byte[] data = new byte[64];
                Array.Copy(tx.SigningHash.Bytes, data, 32);
                Array.Copy(tx.TInputs[i].Hash(tinVals[i]).Bytes, 0, data, 32, 32);
                Cipher.SHA256 checkSig = Cipher.SHA256.HashData(data);

                if (!tx.TSignatures[i].Verify(checkSig)) return new VerifyException("FullTransaction", $"Transparent input at index {i} has invalid signature");
            }

            /* verify triptych signatures */
            for (int i = 0; i < npin; i++)
            {
                if (tx.PInputs[i].Offsets.Length != 64) return new VerifyException("FullTransaction", $"Private input at index {i} has an anonymity set of size {tx.PInputs[i].Offsets.Length}; expected 64");

                Cipher.Key[] M = mixins[i].Select(x => x.UXKey).ToArray();
                Cipher.Key[] P = mixins[i].Select(x => x.Commitment).ToArray();

                var sigexc = tx.PSignatures[i].Verify(M, P, tx.PseudoOutputs[i], tx.SigningHash.ToKey(), tx.PInputs[i].KeyImage);
                if (sigexc != null) return sigexc;
            }

            return null;
        }

        public Exception ProcessTx(FullTransaction tx)
        {
            MemTx mtx = new MemTx { Received = DateTime.UtcNow.Ticks, Tx = tx };
            var exc = CheckTx(tx);
            if (exc != null) return exc;

            AddToPool(mtx);

            bool orph = orphanTxs.TryRemove(tx.TxID, out FullTransaction orphan);

            if (orph)
            {
                ProcessTx(orphan);
            }

            return null;
        }
    }
}
