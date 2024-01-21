using Discreet.Coin;
using Discreet.Coin.Models;
using Discreet.Common;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;
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
        private Block block; // for validating single block
        private List<Block> blocks; // for validating multiple blocks

        public Block CurBlock { get { return block; } }

        /* validation and update data */
        public bool valid;
        public List<UpdateEntry> updates;
        private uint pIndex;
        private ulong tIndex;

        /* ephemeral validation data structures */
        private Dictionary<TTXInput, TTXOutput> spentPubs;
        private Dictionary<TTXInput, TTXOutput> newOutputs;
        private SortedSet<Cipher.Key> spentKeys;
        private SortedSet<Cipher.Key> txs;

        private long previousHeight;
        private Dictionary<Cipher.SHA256, Block> blocksCache;
        private Dictionary<uint, TXOutput> outputsCache;

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
            blocksCache = new(new Cipher.SHA256EqualityComparer());
            previousHeight = dataView.GetChainHeight();
            outputsCache = new();
        }

        public ValidationCache(List<Block> blks)
        {
            dataView = DataView.GetView();
            blocks = blks;
            pIndex = dataView.GetOutputIndex();
            tIndex = dataView.GetTransactionIndexer();
            updates = new List<UpdateEntry>();

            spentPubs = new();
            spentKeys = new(new Cipher.KeyComparer());
            txs = new(new Cipher.KeyComparer());
            newOutputs = new();
            blocksCache = new(new Cipher.SHA256EqualityComparer());
            previousHeight = dataView.GetChainHeight();
            outputsCache = new();
        }

        public Exception Validate()
        {
            if (blocks != null) return validateMany();
            else return validate();
        }

        private Exception validateMany()
        {
            blocks.Sort((x, y) => x.Header.Height.CompareTo(y.Header.Height));

            foreach (var block in blocks)
            {
                this.block = block;
                Exception exc = validate(true);

                if (exc != null) return exc;
            }

            return null;
        }

        public (Exception, long, List<Block>, List<Cipher.SHA256>) ValidateReturnFailures()
        {
            // assume if one block fails, all proceeding blocks fail
            // first sort according to order (by height)
            blocks.Sort((x, y) => x.Header.Height.CompareTo(y.Header.Height));
            List<Block> valid = new();

            for (int i = 0; i < blocks.Count; i++)
            {
                // Daemon.Logger.Debug($"Validating block at height {blocks[i].Header.Height}");
                block = blocks[i];
                Exception exc = validate(true);

                if (exc != null)
                {
                    List<Cipher.SHA256> vecs = new();
                    for (int j = i; j < blocks.Count; j++)
                    {
                        // do it by height; safer
                        vecs.Add(new Cipher.SHA256(blocks[j].Header.Height));
                    }

                    return (exc, block.Header.Height, valid, vecs);
                }
                else
                {
                    valid.Add(block);
                }
            }

            return (null, block.Header.Height, valid, null);
        }

        private Exception validate(bool many = false)
        {
            /* validate basic data */
            if (block.Header.Version != 1 && block.Header.Version != 2) return new VerifyException("Block", $"Unsupported version (blocks are either version 1 or 2); got version {block.Header.Version}");
            if (!block.Hash().Equals(block.Header.BlockHash)) return new VerifyException("Block", $"Block hash in header does not match calculated block hash");
            if (dataView.BlockExists(block.Header.BlockHash)) return new VerifyException("Block", $"Block already present");
            if (block.Transactions == null || block.Transactions.Length == 0) return new VerifyException("Block", "Block contains no transactions");
            if (block.Header.NumTXs != block.Transactions.Length) return new VerifyException("Block", $"Block tx mismatch: expected {block.Header.NumTXs}; got {block.Transactions.Length}");
            if (block.GetSize() != block.Header.BlockSize) return new VerifyException("Block", $"Block size mismatch: expected {block.Header.BlockSize}; got {block.GetSize()}");
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
            if (block.Header.Version == 2 && block.Header.Height > 0)
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
                var minerexc = coinbase.Verify(inBlock: true);

                if (minerexc != null)
                {
                    return minerexc;
                }

                /* now verify output amount matches commitment */
                Cipher.Key feeComm = new(new byte[32]);
                Cipher.Key _I = Cipher.Key.Copy(Cipher.Key.I);
                Cipher.KeyOps.GenCommitment(ref feeComm, ref _I, block.Header.Fee + Config.STANDARD_BLOCK_REWARD);

                if (!feeComm.Equals(coinbase.Outputs[0].Commitment))
                {
                    return new VerifyException("Block", "Coinbase transaction in block does not balance with fee commitment");
                }
            }

            /* pre-check transaction data */
            for (int i = (block.Header.Version == 2) ? 1 : 0; i < block.Transactions.Length; i++)
            {
                var precheckExc = block.Transactions[i].Precheck(mustBeCoinbase: block.Header.Height == 0);
                if (precheckExc != null) return precheckExc;
            }

            /* check signature data */
            if ((block.Header.Version == 1 || block.Header.Version == 2) && !block.CheckSignature())
            {
                return new VerifyException("Block", "Block signature is invalid and/or does not come from a valid block authority");
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
                    var cExc = tx.ToPrivate().Verify(inBlock: true);
                    if (cExc != null) return new VerifyException("Block", $"Transaction {tx.TxID.ToHexShort()} failed verification: {cExc.Message}");

                    txs.Add(tx.TxID.ToKey());
                }
            }
            else
            {
                /* check orphan data */
                if (many)
                {
                    if (blocksCache.Count == 0)
                    {
                        var pbsucc = dataView.TryGetBlockHeader(block.Header.PreviousBlock, out var prevBlockHeader);
                        if (prevBlockHeader == null) return new VerifyException("Block", "Could not get previous block");
                        if (prevBlockHeader.Height + 1 != block.Header.Height) return new VerifyException("Block", "Previous block height + 1 does not equal block height");
                        if (previousHeight + 1 != block.Header.Height) return new VerifyException("Block", "Chain height + 1 does not equal block height");
                    }
                    else
                    {
                        var pbsucc = blocksCache.TryGetValue(block.Header.PreviousBlock, out var prevBlock);
                        if (!pbsucc || prevBlock == null) return new VerifyException("Block", "Could not get previous block");
                        if (prevBlock.Header.Height + 1 != block.Header.Height) return new VerifyException("Block", "Previous block height + 1 does not equal block height");
                        if (previousHeight + 1 != block.Header.Height) return new VerifyException("Block", "Chain height + 1 does not equal block height");
                    }
                }
                else
                {
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
                }
                

                for (int i = block.Header.Version == 1 ? 0 : 1; i < block.Transactions.Length; i++)
                {
                    var tx = block.Transactions[i];

                    /* reject if duplicate or in main branch */
                    if (txs.Contains(tx.TxID.ToKey())) return new VerifyException("Block", $"Transaction {tx.TxID.ToHexShort()} already present in block");
                    if (dataView.ContainsTransaction(tx.TxID)) return new VerifyException("Block", $"Transaction {tx.TxID.ToHexShort()} already present in main branch");

                    /* transparent checks */
                    TTXOutput[] tinVals = new TTXOutput[tx.NumTInputs];
                    if (tx.NumTInputs > 0)
                    {
                        /* check for spend in pool */
                        HashSet<TTXInput> _in = new HashSet<TTXInput>(new Coin.Comparers.TTXInputEqualityComparer());
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
                            if (!dataView.CheckSpentKey(tx.PInputs[j].KeyImage) || spentKeys.Contains(tx.PInputs[j].KeyImage)) return new VerifyException("Block", $"Private input's key image ({tx.PInputs[j].KeyImage.ToHexShort()}) already spent");

                            /* verify existence of all mixins */
                            if (many)
                            {
                                mixins[j] = new TXOutput[64];
                                for (int k = 0; k < 64; k++)
                                {
                                    bool mixinSuccess = outputsCache.TryGetValue(tx.PInputs[j].Offsets[k], out mixins[j][k]);
                                    if (!mixinSuccess)
                                    {
                                        try
                                        {
                                            mixins[j][k] = dataView.GetOutput(tx.PInputs[j].Offsets[k]);
                                        }
                                        catch
                                        {
                                            return new VerifyException("Block", $"Private input at index {j} has invalid or missing mixin data");
                                        }
                                    }
                                }
                            }
                            else
                            {
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
                            return new VerifyException("Block", $"Transparent input sum resulted in overflow");
                        }
                    }

                    /* calculate toutAmt + Fee */
                    ulong toutAmt = 0;
                    if (tx.NumTOutputs > 0)
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
                    for (int j = 0; j < npsout; j++)
                    {
                        Cipher.KeyOps.AddKeys(ref tmp, ref pinAmt, ref tx.PseudoOutputs[j]);
                        Array.Copy(tmp.bytes, pinAmt.bytes, 32);
                    }

                    /* calculate poutAmt */
                    Cipher.Key poutAmt = new(new byte[32]);
                    for (int j = 0; j < tx.NumPOutputs; j++)
                    {
                        var comm = tx.POutputs[j].Commitment;
                        Cipher.KeyOps.AddKeys(ref tmp, ref poutAmt, ref comm);
                        Array.Copy(tmp.bytes, poutAmt.bytes, 32);
                    }

                    /* commit + sum tinAmt to pinAmt */
                    Cipher.Key _N = new(new byte[32]);
                    _N.bytes[0] = (byte)((tx.NumTInputs > 0 && tx.NumPOutputs > 0) ? tx.NumPOutputs : 0);
                    Cipher.Key inAmt = new(new byte[32]);
                    Cipher.KeyOps.GenCommitment(ref tmp, ref _N, tinAmt);
                    Cipher.KeyOps.AddKeys(ref inAmt, ref pinAmt, ref tmp);

                    /* commit + sum toutAmt to poutAmt */
                    Cipher.Key _Z = Cipher.Key.Copy(Cipher.Key.Z);
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
                            var newOut = new TTXInput { TxSrc = tx.TxID, Offset = (byte)j };
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

            if (many)
            {
                blocksCache.Add(block.Header.BlockHash, block);
                previousHeight = block.Header.Height;
            }

            Dictionary<TTXInput, TTXOutput> pubUpdates = new();

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

                    if (many)
                    {
                        outputsCache.Add(pIndex, tx.POutputs[i]);
                    }
                }

                updates.Add(new UpdateEntry { key = tx.TxID.Bytes, value = Serialization.UInt32Array(uintArr), rule = UpdateRule.ADD, type = UpdateType.OUTPUTINDICES });

                /* spent keys */
                for (int i = 0; i < tx.NumPInputs; i++)
                {
                    updates.Add(new UpdateEntry { key = tx.PInputs[i].KeyImage.bytes, value = ChainDB.ZEROKEY, rule = UpdateRule.ADD, type = UpdateType.SPENTKEY });
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
                        updates.Add(new UpdateEntry { key = tx.TInputs[i].Serialize(), value = ChainDB.ZEROKEY, rule = UpdateRule.DEL, type = UpdateType.PUBOUTPUT });
                    }
                }

                /* toutputs */
                for (int i = 0; i < tx.NumTOutputs; i++)
                {
                    pubUpdates[new TTXInput { TxSrc = tx.TxID, Offset = (byte)i }] = tx.TOutputs[i];
                }
            }

            /* new toutputs */
            foreach ((var txi, var txo) in pubUpdates) 
            {
                txo.TransactionSrc = txi.TxSrc;
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
            if (blocks != null)
            {
                foreach (var block in blocks)
                {
                    Daemon.TXPool.GetTXPool().UpdatePool(block.Transactions);
                }
            }
            else
            {
                Daemon.TXPool.GetTXPool().UpdatePool(block.Transactions);
            }
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
