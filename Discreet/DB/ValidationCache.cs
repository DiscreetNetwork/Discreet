using Discreet.Coin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.DB
{
    public class ValidationCache
    {
        /* raw data and db access */
        private DataView dataView;
        private Block block;
        
        /* validation and update data */
        public bool valid;
        public List<UpdateEntry> updates;
        private uint pIndex;
        private ulong tIndex;

        /* ephemeral validation data structures */
        private Dictionary<Coin.Transparent.TXInput, Coin.Transparent.TXOutput> spentPubs;
        private Dictionary<Coin.Transparent.TXInput, Coin.Transparent.TXOutput> newOutputs;
        private SortedSet<Cipher.Key> spentKeys;
        private SortedSet<Cipher.Key> txs;

        public ValidationCache(Block blk)
        {
            dataView = DataView.GetView();
            block = blk;
            pIndex = dataView.GetOutputIndex();
            tIndex = dataView.GetTransactionIndexer();
            updates = new List<UpdateEntry>();

            spentPubs = new();
            spentKeys = new(new Cipher.KeyComparer());
            txs = new(new Cipher.KeyComparer());
            newOutputs = new();
        }

        public Exception Validate()
        {
            /* validate basic data */
            if (block.Header.Version != 1 && block.Header.Version != 2) return new VerifyException("Block", $"Unsupported version (blocks are either version 1 or 2); got version {block.Header.Version}");
            if (!block.Hash().Equals(block.Header.BlockHash)) return new VerifyException("Block", $"Block hash in header does not match calculated block hash");
            if (dataView.BlockExists(block.Header.BlockHash)) return new VerifyException("Block", $"Block already present");
            if (block.Transactions == null || block.Transactions.Length == 0) return new VerifyException("Block", "Block contains no transactions");
            if (block.Header.NumTXs != block.Transactions.Length) return new VerifyException("Block", $"Block tx mismatch: expected {block.Header.NumTXs}; got {block.Transactions.Length}");
            if (block.Size() != block.Header.BlockSize) return new VerifyException("Block", $"Block size mismatch: expected {block.Header.BlockSize}; got {block.Size()}");
            if ((long)block.Header.Timestamp > DateTime.UtcNow.AddMinutes(120).Ticks) return new VerifyException("Block", $"Block timestamp is more than two hours in the future");
            if (!block.GetMerkleRoot().Equals(block.Header.MerkleRoot)) return new VerifyException("Block", $"Block merkle hash does not match calculated block merkle hash");
            if (block.Header.ExtraLen != (block.Header.Extra?.Length ?? 0)) return new VerifyException("Block", $"Block extra mismatch: expected length {block.Header.ExtraLen}, but got {block.Header.Extra?.Length ?? 0}");

            /* check fee and private output count */
            ulong fee = 0;
            uint numOutputs = 0;

            for (int i = 0; i < block.Transactions.Length; i++)
            {
                fee += block.Transactions[i].Fee;
                numOutputs += block.Transactions[i].NumPOutputs;
            }

            if (fee != block.Header.Fee) return new VerifyException("Block", $"Block fee mismatch: expected {block.Header.Fee}; got {fee} from calculations");
            if (numOutputs != block.Header.NumOutputs) return new VerifyException("Block", $"block private output count mismatch: expected {block.Header.NumOutputs}; got {numOutputs} from calculations");

            /* check coinbase */
            if (block.Header.Version == 2)
            {
                var coinbaseTx = block.Transactions[0];

                if (coinbaseTx.Version != 0)
                {
                    return new VerifyException("Block", "Miner tx not present or invalid");
                }

                var coinbase = coinbaseTx.ToPrivate();

                if (coinbase.Outputs == null || coinbase.Outputs.Length != 1)
                {
                    return new VerifyException("Block", "Miner tx has invalid outputs");
                }

                /* coinbase tx has special verification logic */
                var minerexc = coinbase.Verify();

                if (minerexc != null)
                {
                    return minerexc;
                }

                /* now verify output amount matches commitment */
                Cipher.Key feeComm = new(new byte[32]);
                Cipher.Key _I = Cipher.Key.Copy(Cipher.Key.I);
                Cipher.KeyOps.GenCommitment(ref feeComm, ref _I, block.Header.Fee);

                if (!feeComm.Equals(coinbase.Outputs[0].Commitment))
                {
                    return new VerifyException("Block", "Coinbase transaction in block does not balance with fee commitment");
                }
            }

            /* pre-check transaction data */
            for (int i = (block.Header.Version == 2) ? 1 : 0; i < block.Transactions.Length; i++)
            {
                var precheckExc = block.Transactions[i].Precheck();
                if (precheckExc != null) return precheckExc;
            }

            /* check signature data */
            if ((block.Header.Version == 1 || block.Header.Version == 2) && !block.CheckSignature())
            {
                return new VerifyException("Block", "Block signature is invalid and/or does not come from a masternode");
            }

            /* special rules for genesis block */
            if (block.Header.Height == 0)
            {
                if (!block.Header.PreviousBlock.Equals(new Cipher.SHA256(new byte[32], false)))
                {
                    return new VerifyException("Block", "Block at height 0 must point to zero hash");
                }

                for (int i = block.Header.Version == 1 ? 0 : 1; i < block.Transactions.Length; i++)
                {
                    var tx = block.Transactions[i];

                    if (txs.Contains(tx.TxID.ToKey())) return new VerifyException("Block", $"Transaction {tx.TxID.ToHexShort()} already present in block");
                    var cExc = tx.ToPrivate().Verify();
                    if (cExc != null) return new VerifyException("Block", $"Transaction {tx.TxID.ToHexShort()} failed verification: {cExc.Message}");

                    txs.Add(tx.TxID.ToKey());
                }
            }
            else
            {
                /* check orphan data */
                if (!dataView.BlockExists(block.Header.PreviousBlock))
                {
                    return new OrphanBlockException("Orphan block detected", dataView.GetChainHeight(), block.Header.Height, block);
                }
                else
                {
                    var prevHeader = dataView.GetBlockHeader(block.Header.PreviousBlock);
                    if (prevHeader.Height + 1 != block.Header.Height) return new VerifyException("Block", "Previous block height + 1 does not equal block height");
                    if (dataView.GetChainHeight() + 1 != block.Header.Height) return new VerifyException("Block", "Chain height + 1 does not equal block height");
                }

                for (int i = block.Header.Version == 1 ? 0 : 1; i < block.Transactions.Length; i++)
                {
                    var tx = block.Transactions[i];

                    /* reject if duplicate or in main branch */
                    if (txs.Contains(tx.TxID.ToKey())) return new VerifyException("Block", $"Transaction {tx.TxID.ToHexShort()} already present in block");
                    if (dataView.ContainsTransaction(tx.TxID)) return new VerifyException("Block", $"Transaction {tx.TxID.ToHexShort()} already present in main branch");

                    /* transparent checks */
                    Coin.Transparent.TXOutput[] tinVals = new Coin.Transparent.TXOutput[tx.NumTInputs];
                    if (tx.NumTInputs > 0)
                    {
                        /* check for spend in pool */
                        HashSet<Coin.Transparent.TXInput> _in = new HashSet<Coin.Transparent.TXInput>(new Coin.Transparent.TXInputEqualityComparer());
                        for (int j = 0; j < tx.NumTInputs; j++)
                        {
                            _in.Add(tx.TInputs[j]);

                            if (spentPubs.ContainsKey(tx.TInputs[j])) return new VerifyException("Block", $"Transparent input at index {j} was spent in a previous transaction currently in the validation set");
                        }
                    }

                    /* more transparent checks */
                    if (tx.NumTInputs > 0)
                    {
                        /* check orphan data */
                        HashSet<Cipher.SHA256> missingTxs = new HashSet<Cipher.SHA256>();
                        for (int j = 0; j < tx.NumTInputs; j++)
                        {
                            if (!txs.Contains(tx.TInputs[j].TxSrc.ToKey()) && !dataView.ContainsTransaction(tx.TInputs[j].TxSrc)) missingTxs.Add(tx.TInputs[j].TxSrc);
                        }

                        /* reject if missing outputs */
                        if (missingTxs.Count > 0)
                        {
                            return new VerifyException("Block", $"Transaction is missing outputs");
                        }

                        /* grab matching outputs for inputs */
                        for (int j = 0; j < tx.NumTInputs; j++)
                        {
                            bool res = newOutputs.TryGetValue(tx.TInputs[j], out tinVals[j]);
                            if (!res)
                            {
                                try
                                {
                                    tinVals[j] = dataView.GetPubOutput(tx.TInputs[j]);
                                }
                                catch
                                {
                                    return new VerifyException("Block", $"Transaction transparent input at index {j} has no matching output (double spend or fake)");
                                }
                            }
                        }

                        /* check address info */
                        for (int j = 0; j < tx.NumTInputs; j++)
                        {
                            var aexc = tinVals[j].Address.Verify();
                            if (aexc != null) return aexc;

                            if (!tinVals[j].Address.CheckAddressBytes(tx.TSignatures[j].y))
                            {
                                return new VerifyException("FullTransaction", $"Transparent input at index {j}'s address ({tinVals[j].Address}) does not match public key in signature ({tx.TSignatures[j].y.ToHexShort()})");
                            }
                        }
                    }

                    /* private checks */
                    var npsout = (tx.PseudoOutputs == null) ? 0 : tx.PseudoOutputs.Length;
                    TXOutput[][] mixins = new TXOutput[tx.NumPInputs][];
                    if (tx.NumPInputs > 0)
                    {
                        var uncheckedTags = new List<Cipher.Key>(tx.PInputs.Select(x => x.KeyImage));

                        for (int j = 0; j < tx.NumPInputs; j++)
                        {
                            /* verify no duplicate spends in pool or main branch */
                            if (!dataView.CheckSpentKey(tx.PInputs[j].KeyImage) || !spentKeys.Contains(tx.PInputs[j].KeyImage)) return new VerifyException("Block", $"Private input's key image ({tx.PInputs[j].KeyImage.ToHexShort()}) already spent");

                            /* verify existence of all mixins */
                            try
                            {
                                mixins[j] = dataView.GetMixins(tx.PInputs[j].Offsets);
                            }
                            catch
                            {
                                return new VerifyException("Block", $"Private input at index {j} has invalid or missing mixin data");
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
                            return new VerifyException("Block", $"Transparent input sum resulted in overflow");
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
                            return new VerifyException("Block", $"Transparent output sum resulted in overflow");
                        }
                    }
                    try
                    {
                        toutAmt = checked(tx.Fee + toutAmt);
                    }
                    catch (OverflowException)
                    {
                        return new VerifyException("Block", $"Transaction fee + output sum resulted in overflow");
                    }

                    Cipher.Key tmp = new(new byte[32]);

                    /* calculate pinAmt */
                    Cipher.Key pinAmt = new(new byte[32]);
                    for (int j = 0; j < npsout; i++)
                    {
                        Cipher.KeyOps.AddKeys(ref tmp, ref pinAmt, ref tx.PseudoOutputs[j]);
                        Array.Copy(tmp.bytes, pinAmt.bytes, 32);
                    }

                    /* calculate poutAmt */
                    Cipher.Key poutAmt = new(new byte[32]);
                    for (int j = 0; j < tx.NumPOutputs; j++)
                    {
                        Cipher.KeyOps.AddKeys(ref tmp, ref poutAmt, ref tx.POutputs[j].Commitment);
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
                    if (!inAmt.Equals(outAmt)) return new VerifyException("Block", $"Transaction does not balance");

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
                    for (int j = 0; j < tx.NumTInputs; j++)
                    {
                        if (tx.TSignatures[j].IsNull()) return new VerifyException("Block", $"Unsigned transparent input at index {j}");

                        byte[] data = new byte[64];
                        Array.Copy(tx.SigningHash.Bytes, data, 32);
                        Array.Copy(tx.TInputs[j].Hash(tinVals[j]).Bytes, 0, data, 32, 32);
                        Cipher.SHA256 checkSig = Cipher.SHA256.HashData(data);

                        if (!tx.TSignatures[j].Verify(checkSig)) return new VerifyException("Block", $"Transparent input at index {j} has invalid signature");
                    }

                    /* verify triptych signatures */
                    for (int j = 0; j < tx.NumPInputs; j++)
                    {
                        if (tx.PInputs[j].Offsets.Length != 64) return new VerifyException("Block", $"Private input at index {j} has an anonymity set of size {tx.PInputs[j].Offsets.Length}; expected 64");

                        Cipher.Key[] M = mixins[j].Select(x => x.UXKey).ToArray();
                        Cipher.Key[] P = mixins[j].Select(x => x.Commitment).ToArray();

                        var sigexc = tx.PSignatures[j].Verify(M, P, tx.PseudoOutputs[j], tx.SigningHash.ToKey(), tx.PInputs[j].KeyImage);
                        if (sigexc != null) return sigexc;
                    }

                    /* valid; update validation structures */
                    txs.Add(tx.TxID.ToKey());

                    if (tx.NumTInputs > 0)
                    {
                        for (int j = 0; j < tx.NumTInputs; j++)
                        {
                            if (newOutputs.TryGetValue(tx.TInputs[j], out var txo))
                            {
                                newOutputs.Remove(tx.TInputs[j], out _);
                                spentPubs[tx.TInputs[j]] = txo;
                            }
                            else
                            {
                                spentPubs[tx.TInputs[j]] = tinVals[j];
                            }
                        }
                    }

                    if (tx.NumTOutputs > 0)
                    {
                        for (int j = 0; j < tx.TOutputs.Length; j++)
                        {
                            var newOut = new Coin.Transparent.TXInput(tx.TxID, (byte)j);
                            newOutputs[newOut] = tx.TOutputs[j];
                        }
                    }

                    if (tx.NumPInputs > 0)
                    {
                        for (int j = 0; j < tx.PInputs.Length; j++)
                        {
                            spentKeys.Add(tx.PInputs[j].KeyImage);
                        }
                    }
                }
            }

            /* now build update rules */
            /* Block info */
            updates.Add(new UpdateEntry { key = block.Header.BlockHash.Bytes, value = Serialization.Int64(block.Header.Height), rule = UpdateRule.ADD, type = UpdateType.BLOCKHEIGHT });
            updates.Add(new UpdateEntry { key = Serialization.Int64(block.Header.Height), value = block.Serialize(), rule = UpdateRule.ADD, type = UpdateType.BLOCK });
            updates.Add(new UpdateEntry { key = Serialization.Int64(block.Header.Height), value = block.Header.Serialize(), rule = UpdateRule.ADD, type = UpdateType.BLOCKHEADER });

            Dictionary<Coin.Transparent.TXInput, Coin.Transparent.TXOutput> pubUpdates = new();

            /* txs */
            foreach (var tx in block.Transactions)
            {
                tIndex++;
                updates.Add(new UpdateEntry { key = tx.TxID.Bytes, value = Serialization.UInt64(tIndex), rule = UpdateRule.ADD, type = UpdateType.TXINDEX });
                updates.Add(new UpdateEntry { key = Serialization.UInt64(tIndex), value = tx.Serialize(), rule = UpdateRule.ADD, type = UpdateType.TX });

                /* private outputs */
                uint[] uintArr = new uint[tx.NumPOutputs];
                for (int i = 0; i < uintArr.Length; i++)
                {
                    pIndex++;
                    tx.POutputs[i].TransactionSrc = tx.TxID;
                    updates.Add(new UpdateEntry { key = Serialization.UInt32(pIndex), value = tx.POutputs[i].Serialize(), rule = UpdateRule.ADD, type = UpdateType.OUTPUT });
                    uintArr[i] = pIndex;
                }

                updates.Add(new UpdateEntry { key = tx.TxID.Bytes, value = Serialization.UInt32Array(uintArr), rule = UpdateRule.ADD, type = UpdateType.OUTPUTINDICES });

                /* spent keys */
                for (int i = 0; i < tx.NumPInputs; i++)
                {
                    updates.Add(new UpdateEntry { key = tx.PInputs[i].KeyImage.bytes, value = StateDB.ZEROKEY, rule = UpdateRule.ADD, type = UpdateType.SPENTKEY });
                }

                /* tinputs */
                for (int i = 0; i < tx.NumTInputs; i++)
                {
                    if (pubUpdates.ContainsKey(tx.TInputs[i]))
                    {
                        pubUpdates.Remove(tx.TInputs[i]);
                    }
                    else
                    {
                        updates.Add(new UpdateEntry { key = tx.TInputs[i].Serialize(), value = StateDB.ZEROKEY, rule = UpdateRule.DEL, type = UpdateType.PUBOUTPUT });
                    }
                }

                /* toutputs */
                for (int i = 0; i < tx.NumTOutputs; i++)
                {
                    pubUpdates[new Coin.Transparent.TXInput(tx.TxID, (byte)i)] = tx.TOutputs[i];
                }
            }

            /* new toutputs */
            foreach ((var txi, var txo) in pubUpdates) 
            {
                updates.Add(new UpdateEntry { key = txi.Serialize(), value = txo.Serialize(), rule = UpdateRule.ADD, type = UpdateType.PUBOUTPUT });
            }

            /* final items */
            updates.Add(new UpdateEntry { key = Encoding.ASCII.GetBytes("indexer_tx"), value = Serialization.UInt64(tIndex), rule = UpdateRule.UPDATE, type = UpdateType.TXINDEXER });
            updates.Add(new UpdateEntry { key = Encoding.ASCII.GetBytes("indexer_output"), value = Serialization.UInt32(pIndex), rule = UpdateRule.UPDATE, type = UpdateType.OUTPUTINDEXER });
            updates.Add(new UpdateEntry { key = Encoding.ASCII.GetBytes("height"), value = Serialization.Int64(block.Header.Height), rule = UpdateRule.UPDATE, type = UpdateType.HEIGHT });

            return null;
        }

        public void Flush()
        {
            dataView.Flush(updates);
            Daemon.TXPool.GetTXPool().UpdatePool(block.Transactions);
        }

        public static void Process(Block block)
        {
            ValidationCache vCache = new ValidationCache(block);
            var exc = vCache.Validate();
            if (exc != null) throw exc;
            vCache.Flush();
        }
    }
}
