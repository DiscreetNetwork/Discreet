using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using Discreet.Common;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;
using System.Text.Json;

namespace Discreet.Coin.Models
{
    public class Transaction : IHashable
    {
        public byte Version { get; set; }
        public byte NumInputs { get; set; }
        public byte NumOutputs { get; set; }
        public byte NumSigs { get; set; }

        public ulong Fee { get; set; }
        public Cipher.Key TransactionKey { get; set; }

        public TXInput[] Inputs { get; set; }
        public TXOutput[] Outputs { get; set; }

        public Bulletproof RangeProof { get; set; }
        public BulletproofPlus RangeProofPlus { get; set; }

        public Triptych[] Signatures { get; set; }
        public Cipher.Key[] PseudoOutputs { get; set; }

        private Cipher.SHA256 _txid;
        public Cipher.SHA256 TxID { get { if (_txid == default || _txid.Bytes == null) _txid = this.Hash(); return _txid; } }

        public Transaction() { }

        public Transaction(byte[] bytes)
        {
            var reader = new MemoryReader(bytes.AsMemory());
            this.Deserialize(ref reader);
        }

        public FullTransaction ToFull()
        {
            return new FullTransaction(this);
        }


        /**
         * This is the hash signed through signatures.
         * It is composed of:
         *  Version,
         *  Inputs,
         *  Outputs (only output pubkey and amount),
         *  Extra
         */
        public Cipher.SHA256 SigningHash()
        {
            byte[] bytes = new byte[1 + Inputs.Length * TXInput.GetSize() + (32 + 8) * Outputs.Length + 32];
            var writer = new BEBinaryWriter(new MemoryStream(bytes));

            writer.Write(Version);
            writer.WriteSerializableArray(Inputs, false);
            writer.WriteSerializableArray(Outputs, false, (x) => (writer) => { writer.WriteKey(x.UXKey); writer.Write(x.Amount); });
            writer.WriteKey(TransactionKey);

            return Cipher.SHA256.HashData(bytes);
        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(NumInputs);
            writer.Write(NumOutputs);
            writer.Write(NumSigs);

            if (Version > 0) writer.Write(Fee);
            writer.WriteKey(TransactionKey);

            if (Version > 0) writer.WriteSerializableArray(Inputs, false);
            writer.WriteSerializableArray(Outputs, false, (x) => x.TXMarshal);

            if (Version > 0)
            {
                if (Version == 2) RangeProofPlus.Serialize(writer);
                else RangeProof.Serialize(writer);

                writer.WriteSerializableArray(Signatures, false);
                writer.WriteKeyArray(PseudoOutputs, false);
            }
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Version = reader.ReadUInt8();
            NumInputs = reader.ReadUInt8();
            NumOutputs = reader.ReadUInt8();
            NumSigs = reader.ReadUInt8();

            if (Version > 0) Fee = reader.ReadUInt64();
            TransactionKey = reader.ReadKey();

            if (Version > 0) Inputs = reader.ReadSerializableArray<TXInput>(NumInputs);
            Outputs = reader.ReadSerializableArray<TXOutput>(NumOutputs, (x) => x.TXUnmarshal);

            if (Version > 0)
            {
                if (Version == 2) RangeProofPlus = reader.ReadSerializable<BulletproofPlus>();
                else RangeProof = reader.ReadSerializable<Bulletproof>();

                Signatures = reader.ReadSerializableArray<Triptych>(NumSigs);
                PseudoOutputs = reader.ReadKeyArray(NumInputs);
            }
        }

        public string Readable()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Converters.TransactionConverter());

            return JsonSerializer.Serialize(ToFull(), typeof(FullTransaction), options);
        }

        /* for testing purposes only */
        public static Transaction GenerateRandomNoSpend(StealthAddress to, int numOutputs)
        {
            Transaction tx = new()
            {
                Version = 0,
                NumInputs = 0,
                NumOutputs = (byte)numOutputs,
                NumSigs = 0,
            };

            Cipher.Key r = new(new byte[32]);
            Cipher.Key R = new(new byte[32]);

            Cipher.KeyOps.GenerateKeypair(ref r, ref R);

            tx.Outputs = new TXOutput[numOutputs];

            for (int i = 0; i < numOutputs; i++)
            {
                tx.Outputs[i] = new TXOutput
                {
                    Commitment = new Cipher.Key(new byte[32])
                };
                Cipher.Key mask = Cipher.KeyOps.GenCommitmentMask(ref r, ref to.view, i);
                /* the amount is randomly generated between 1 and 100 DIS (droplet size is 10^10) */
                ulong amnt = 1_000_000_000_0 + Cipher.KeyOps.RandomDisAmount(99_000_000_000_0);
                var comm = tx.Outputs[i].Commitment;
                Cipher.KeyOps.GenCommitment(ref comm, ref mask, amnt);
                tx.Outputs[i].UXKey = Cipher.KeyOps.DKSAP(ref r, to.view, to.spend, i);
                tx.Outputs[i].Amount = Cipher.KeyOps.GenAmountMask(ref r, ref to.view, i, amnt);
            }

            tx.TransactionKey = R;

            return tx;
        }

        public static Transaction GenerateTransactionNoSpend(StealthAddress to, ulong amnt)
        {
            Transaction tx = new()
            {
                Version = 0,
                NumInputs = 0,
                NumOutputs = 1,
                NumSigs = 0,
            };

            Cipher.Key r = new(new byte[32]);
            Cipher.Key R = new(new byte[32]);

            Cipher.KeyOps.GenerateKeypair(ref r, ref R);

            tx.Outputs = new TXOutput[1];

            tx.Outputs[0] = new TXOutput
            {
                Commitment = new Cipher.Key(new byte[32])
            };
            Cipher.Key mask = Cipher.KeyOps.GenCommitmentMask(ref r, ref to.view, 0);
            var comm = tx.Outputs[0].Commitment;
            Cipher.KeyOps.GenCommitment(ref comm, ref mask, amnt);
            tx.Outputs[0].UXKey = Cipher.KeyOps.DKSAP(ref r, to.view, to.spend, 0);
            tx.Outputs[0].Amount = Cipher.KeyOps.GenAmountMask(ref r, ref to.view, 0, amnt);

            tx.TransactionKey = R;

            return tx;
        }

        public Cipher.Key[] GetCommitments()
        {
            Cipher.Key[] comms = new Cipher.Key[NumOutputs];

            for (int i = 0; i < NumOutputs; i++)
            {
                comms[i] = Outputs[i].Commitment;
            }

            return comms;
        }

        public uint GetSize()
        {
            if (Version == 0)
            {
                return (uint)(4 + 32 + 72 * Outputs.Length);
            }
            else if (Version == 2)
            {
                return (uint)(4 + TXInput.GetSize() * Inputs.Length + 72 * Outputs.Length + RangeProofPlus.GetSize() + 8 + Triptych.GetSize() * Signatures.Length + 32 * PseudoOutputs.Length + 32);
            }
            return (uint)(4 + TXInput.GetSize() * Inputs.Length + 72 * Outputs.Length + RangeProof.GetSize() + 8 + Triptych.GetSize() * Signatures.Length + 32 * PseudoOutputs.Length + 32);
        }

        public int Size => (int)GetSize();

        public VerifyException Verify()
        {
            return Verify(inBlock: false);
        }

        /* this function must be run PRIOR to adding a transaction to the TXPool. 
         * This is not run, and should never be run, on transactions already present in the TXPool.
         * This is definitely never used for transactions already assigned an ID in the database.
         */
        public VerifyException Verify(bool inBlock = false)
        {
            DB.DataView dataView = DB.DataView.GetView();

            /* this function is the most important one for coin logic. We must verify everything. */


            if (NumOutputs != Outputs.Length)
            {
                return new VerifyException("Transaction", $"Output length mismatch: expected {NumOutputs}, but got {Outputs.Length}");
            }

            if (NumOutputs > 16)
            {
                return new VerifyException("Transaction", $"Transactions cannot have more than 16 outputs");
            }

            if (NumOutputs == 0)
            {
                return new VerifyException("Transaction", "Transactions cannot have zero outputs");
            }

            if (!inBlock && Version == 0)
            {
                return new VerifyException("Transaction", "Cannot have a coinbase transaction outside of block!");
            }

            if (Version != 0)
            {
                if (NumInputs != Inputs.Length)
                {
                    return new VerifyException("Transaction", $"Input length mismatch: expected {NumInputs}, but got {Inputs.Length}");
                }

                if (NumSigs != Signatures.Length)
                {
                    return new VerifyException("Transaction", $"Signature length mismatch: expected {NumSigs}, but got {Signatures.Length}");
                }

                if (NumInputs == 0)
                {
                    return new VerifyException("Transaction", $"Transaction has no inputs!");
                }
            }

            if (NumSigs != NumInputs)
            {
                return new VerifyException("Transaction", $"Transactions must have the same number of signatures as inputs ({NumInputs} inputs, but {NumSigs} sigs)");
            }

            if (Version == 0 && NumInputs > 0)
            {
                return new VerifyException("Transaction", $"Coinbase transactions cannot take in inputs!");
            }

            if (NumOutputs == 0)
            {
                return new VerifyException("Transaction", $"Transaction has no outputs!");
            }

            /* this will need to be changed as Version changes */
            if (Version != 1 && Version != 0 && Version != 2)
            {
                return new VerifyException("Transaction", $"Unknown transaction version: {Version} (currently only private transactions, version 1 and 2, and coinbase transactions, version 0, are supported)");
            }

            for (int i = 0; i < NumOutputs; i++)
            {
                //we no longer need to validate these fields.
                /*if (Outputs[i].Amount == 0)
                {
                    return new VerifyException("Transaction", $"Amount field in output at index {i} is not set!");
                }

                if (Outputs[i].Commitment.Equals(Cipher.Key.Z))
                {
                    return new VerifyException("Transaction", $"Commitment field in output at index {i} is not set!");
                }*/
            }

            if (Version == 0)
            {
                /* should be all set */
            }
            else
            {
                /* validate amounts */
                if (PseudoOutputs.Length != NumInputs)
                {
                    return new VerifyException("Transaction", $"PseudoOutput length mismatch: expected {NumInputs}, but got {PseudoOutputs.Length}");
                }

                Cipher.Key sumPseudos = new(new byte[32]);
                Cipher.Key tmp = new(new byte[32]);

                for (int i = 0; i < PseudoOutputs.Length; i++)
                {
                    Cipher.KeyOps.AddKeys(ref tmp, ref sumPseudos, ref PseudoOutputs[i]);
                    Array.Copy(tmp.bytes, sumPseudos.bytes, 32);
                }

                Cipher.Key sumComms = new(new byte[32]);

                for (int i = 0; i < Outputs.Length; i++)
                {
                    var comm = Outputs[i].Commitment;
                    Cipher.KeyOps.AddKeys(ref tmp, ref sumComms, ref comm);
                    Array.Copy(tmp.bytes, sumComms.bytes, 32);
                }

                Cipher.Key commFee = new(new byte[32]);
                Cipher.Key _Z = Cipher.Key.Copy(Cipher.Key.Z);
                Cipher.KeyOps.GenCommitment(ref commFee, ref _Z, Fee);
                Cipher.KeyOps.AddKeys(ref tmp, ref sumComms, ref commFee);
                Array.Copy(tmp.bytes, sumComms.bytes, 32);

                //Cipher.Key dif = new(new byte[32]);
                //Cipher.KeyOps.SubKeys(ref dif, ref sumPseudos, ref sumComms);
                if (!sumPseudos.Equals(sumComms))
                {
                    return new VerifyException("Transaction", $"Transaction does not balance! (sumC'a != sumCb)");
                }

                /* validate range sig */
                VerifyException bpexc = null;

                if (Version == 2)
                {
                    bpexc = RangeProofPlus.Verify(this);
                }
                else
                {
                    bpexc = RangeProof.Verify(this);
                }


                if (bpexc != null)
                {
                    return bpexc;
                }

                /* validate inputs */
                List<Cipher.Key> uncheckedTags = new List<Cipher.Key>(Inputs.Select(x => x.KeyImage));
                for (int i = 0; i < NumInputs; i++)
                {
                    if (Inputs[i].Offsets.Length != 64)
                    {
                        return new VerifyException("Transaction", $"Input at index {i} has an anonymity set of size {Inputs[i].Offsets.Length}; expected 64");
                    }

                    TXOutput[] mixins;
                    try
                    {
                        mixins = dataView.GetMixins(Inputs[i].Offsets);
                    }
                    catch (Exception e)
                    {
                        return new VerifyException("Transaction", $"Got error when getting mixin data at input at index {i}: " + e.Message);
                    }

                    Cipher.Key[] M = new Cipher.Key[64];
                    Cipher.Key[] P = new Cipher.Key[64];

                    for (int k = 0; k < 64; k++)
                    {
                        M[k] = mixins[k].UXKey;
                        P[k] = mixins[k].Commitment;
                    }

                    var sigexc = Signatures[i].Verify(M, P, PseudoOutputs[i], SigningHash().ToKey(), Inputs[i].KeyImage);

                    if (sigexc != null)
                    {
                        return sigexc;
                    }

                    if (inBlock)
                    {
                        if (!dataView.CheckSpentKey(Inputs[i].KeyImage))
                        {
                            return new VerifyException("Transaction", $"Key image for input at index {i} ({Inputs[i].KeyImage.ToHexShort()}) already spent! (double spend)");
                        }
                    }
                    else
                    {
                        if (!dataView.CheckSpentKey(Inputs[i].KeyImage) || Daemon.TXPool.GetTXPool().ContainsSpentKey(Inputs[i].KeyImage))
                        {
                            return new VerifyException("Transaction", $"Key image for input at index {i} ({Inputs[i].KeyImage.ToHexShort()}) already spent! (double spend)");
                        }
                    }

                    uncheckedTags.Remove(Inputs[i].KeyImage);
                    if (uncheckedTags.Any(x => x == Inputs[i].KeyImage))
                    {
                        return new VerifyException("Transaction", $"Key image for {i} ({Inputs[i].KeyImage.ToHexShort()}) already spent in this transaction! (double spend)");
                    }
                }
            }

            return null;
        }
    }
}
