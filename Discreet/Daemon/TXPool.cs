using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin.Models;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;

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
                byte[] data = new byte[Tx.GetSize() + 8];
                Common.Serialization.CopyData(data, 0, Received);
                Tx.Serialize(data, 8);

                return data;
            }

            public void Deserialize(byte[] data)
            {
                Received = Common.Serialization.GetInt64(data, 0);

                Tx = new FullTransaction();
                Tx.Deserialize<FullTransaction>(data, 8);
            }
        }

        private ConcurrentDictionary<Cipher.SHA256, MemTx> pool;
        private ConcurrentDictionary<TTXInput, TTXOutput> spentOutputs;
        private ConcurrentDictionary<Cipher.SHA256, List<TTXInput>> spentsTx;
        private SortedSet<Cipher.Key> spentKeys;
        private ConcurrentDictionary<Cipher.SHA256, List<Cipher.Key>> updateSpentKeys;
        private ConcurrentDictionary<TTXInput, TTXOutput> newOutputs;
        private ConcurrentDictionary<Cipher.SHA256, List<TTXInput>> updateNewOutputs;

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
            newOutputs = new();
            updateNewOutputs = new();

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

        public void AddToPool(MemTx tx)
        {
            pool[tx.Tx.Hash()] = tx;

            /* update the stuff */
            if (tx.Tx.TInputs != null && tx.Tx.TInputs.Length > 0)
            {
                List<TTXInput> stxs = new List<TTXInput>();

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
                List<TTXInput> newOuts = new List<TTXInput>();

                for (int i = 0; i < tx.Tx.TOutputs.Length; i++)
                {
                    var newOut = new TTXInput { TxSrc = tx.Tx.TxID, Offset = (byte)i };
                    newOuts.Add(newOut);
                    newOutputs[newOut] = tx.Tx.TOutputs[i];
                }

                updateNewOutputs[tx.Tx.TxID] = newOuts;
            }

            if (tx.Tx.PInputs != null && tx.Tx.PInputs.Length > 0)
            {
                List<Cipher.Key> ks = new List<Cipher.Key>();

                lock (spentKeys)
                {
                    foreach (var ptxi in tx.Tx.PInputs)
                    {
                    
                        spentKeys.Add(ptxi.KeyImage);
                        ks.Add(ptxi.KeyImage);
                    }
                }

                updateSpentKeys[tx.Tx.TxID] = ks;
            }
        }

        public bool ContainsSpentKey(Cipher.Key k)
        {
            lock (spentKeys)
            {
                return spentKeys.Contains(k);
            }
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

                if (tx != null && tx.Tx.GetSize() + _sizeTotal < maxBytes)
                {
                    txs.Add(tx.Tx);
                    searchTxs.Remove(tx);

                    _sizeTotal += tx.Tx.GetSize();
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

        public bool ContainsSpent(TTXInput txoidx)
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
                    lock (spentKeys)
                    {
                        foreach (var kv in updateSpentKeys[hash])
                        {
                            spentKeys.Remove(kv);
                        }
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

        public bool TryGetTransaction(Cipher.SHA256 txhash, out FullTransaction tx)
        {
            try
            {
                tx = GetTransaction(txhash);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
                tx = null;
                return false;
            }
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
            TTXOutput[] tinVals = new TTXOutput[ntin];
            if (ntin > 0)
            {
                /* check for spend in pool */
                HashSet<TTXInput> _in = new HashSet<TTXInput>(new Coin.Comparers.TTXInputEqualityComparer());
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
                    if (!view.CheckSpentKey(tx.PInputs[i].KeyImage) || ContainsSpentKey(tx.PInputs[i].KeyImage)) return new VerifyException("FullTransaction", $"Private input's key image ({tx.PInputs[i].KeyImage.ToHexShort()}) already spent");

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
            foreach (TTXOutput output in tinVals)
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
            if (ntout > 0)
            {
                foreach (TTXOutput output in tx.TOutputs)
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
                var comm = tx.POutputs[i].Commitment;
                Cipher.KeyOps.AddKeys(ref tmp, ref poutAmt, ref comm);
                Array.Copy(tmp.bytes, poutAmt.bytes, 32);
            }

            /* commit + sum tinAmt to pinAmt */
            Cipher.Key _N = new(new byte[32]);
            _N.bytes[0] = (byte)((ntin > 0 && npout > 0) ? npout : 0);
            Cipher.Key inAmt = new(new byte[32]);
            Cipher.KeyOps.GenCommitment(ref tmp, ref _N, tinAmt);
            Cipher.KeyOps.AddKeys(ref inAmt, ref pinAmt, ref tmp);

            /* commit + sum toutAmt to poutAmt */
            Cipher.Key _Z = new(new byte[32]);
            Cipher.Key outAmt = new(new byte[32]);
            Cipher.KeyOps.GenCommitment(ref tmp, ref _Z, toutAmt);
            Cipher.KeyOps.AddKeys(ref outAmt, ref poutAmt, ref tmp);

            //Logger.Log($"INAMT: {inAmt.ToHex()}");
            //Logger.Log($"OUTAMT: {outAmt.ToHex()}");
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
