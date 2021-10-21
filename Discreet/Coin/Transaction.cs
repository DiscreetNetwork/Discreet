using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Coin
{
    [StructLayout(LayoutKind.Sequential)]
    public class Transaction: ICoin
    {
        [MarshalAs(UnmanagedType.U1)]
        public byte Version;
        [MarshalAs(UnmanagedType.U1)]
        public byte NumInputs;
        [MarshalAs(UnmanagedType.U1)]
        public byte NumOutputs;
        [MarshalAs(UnmanagedType.U1)]
        public byte NumSigs;

        [MarshalAs(UnmanagedType.U8)]
        public ulong Fee;

        [MarshalAs(UnmanagedType.Struct)]
        public Cipher.Key TransactionKey;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public TXInput[] Inputs;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public TXOutput[] Outputs;

        [MarshalAs(UnmanagedType.Struct)]
        public Bulletproof RangeProof;

        [MarshalAs(UnmanagedType.Struct)]
        public BulletproofPlus RangeProofPlus;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public Triptych[] Signatures;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public Cipher.Key[] PseudoOutputs;

        public Cipher.SHA256 Hash()
        {
            return Cipher.SHA256.HashData(Marshal());
        }

        public Transaction() { }

        public Transaction(byte[] bytes)
        {
            Unmarshal(bytes);
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
            byte[] bytes = new byte[1 + Inputs.Length * TXInput.Size() + (32 + 8) * Outputs.Length + 32];

            bytes[0] = Version;

            uint offset = 1;

            for (int i = 0; i < Inputs.Length; i++)
            {
                Array.Copy(Inputs[i].Marshal(), 0, bytes, offset, TXInput.Size());
                offset += TXInput.Size();
            }

            for (int i = 0; i < Outputs.Length; i++)
            {
                Array.Copy(Outputs[i].UXKey.bytes, 0, bytes, offset, 32);
                offset += 32;

                byte[] amount = BitConverter.GetBytes(Outputs[i].Amount);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(amount);
                }

                Array.Copy(amount, 0, bytes, offset, 8);
                offset += 8;
            }

            Array.Copy(TransactionKey.bytes, 0, bytes, offset, 32);

            return Cipher.SHA256.HashData(bytes);
        }

        public byte[] Marshal()
        {
            uint offset = 4;

            if (Version == 0)
            {
                byte[] bytes = new byte[4 + 32 + NumOutputs * 72];

                bytes[0] = Version;
                bytes[1] = NumInputs;
                bytes[2] = NumOutputs;
                bytes[3] = NumSigs;

                Array.Copy(TransactionKey.bytes, 0, bytes, offset, 32);
                offset += 32;

                for (int i = 0; i < Outputs.Length; i++)
                {
                    Outputs[i].TXMarshal(bytes, offset);
                    offset += 72;
                }

                return bytes;
            }
            else
            {
                byte[] bytes = new byte[Size()];

                bytes[0] = Version;
                bytes[1] = NumInputs;
                bytes[2] = NumOutputs;
                bytes[3] = NumSigs;

                byte[] fee = BitConverter.GetBytes(Fee);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(fee);
                }

                Array.Copy(fee, 0, bytes, offset, 8);
                offset += 8;

                Array.Copy(TransactionKey.bytes, 0, bytes, offset, 32);
                offset += 32;

                for (int i = 0; i < Inputs.Length; i++)
                {
                    Inputs[i].Marshal(bytes, offset);
                    offset += TXInput.Size();
                }

                for (int i = 0; i < Outputs.Length; i++)
                {
                    Outputs[i].TXMarshal(bytes, offset);
                    offset += 72;
                }

                if (Version == 2)
                {
                    RangeProofPlus.Marshal(bytes, offset);
                    offset += RangeProofPlus.Size();
                }
                else
                {
                    RangeProof.Marshal(bytes, offset);
                    offset += RangeProof.Size();
                }

                for (int i = 0; i < NumSigs; i++)
                {
                    Signatures[i].Marshal(bytes, offset);
                    offset += Triptych.Size();
                }

                for (int i = 0; i < PseudoOutputs.Length; i++)
                {
                    Array.Copy(PseudoOutputs[i].bytes, 0, bytes, offset, 32);
                    offset += 32;
                }

                return bytes;
            }
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            byte[] rv = Marshal();

            Array.Copy(rv, 0, bytes, offset, rv.Length);
        }

        public string Readable()
        {
            return Discreet.Readable.Transaction.ToReadable(this);
        }

        public static Transaction FromReadable(string json)
        {
            return Discreet.Readable.Transaction.FromReadable(json);
        }

        public void Unmarshal(byte[] bytes)
        {
            if (bytes[0] == 0)
            {
                Version = bytes[0];
                NumInputs = bytes[1];
                NumOutputs = bytes[2];
                NumSigs = bytes[3];

                uint offset = 4;

                TransactionKey = new Cipher.Key(new byte[32]);
                Array.Copy(bytes, offset, TransactionKey.bytes, 0, 32);
                offset += 32;

                Outputs = new TXOutput[NumOutputs];

                for (int i = 0; i < NumOutputs; i++)
                {
                    Outputs[i] = new TXOutput();

                    Outputs[i].TXUnmarshal(bytes, offset);
                    offset += 72;
                }
            }
            else
            {
                Version = bytes[0];
                NumInputs = bytes[1];
                NumOutputs = bytes[2];
                NumSigs = bytes[3];

                uint offset = 4;

                byte[] fee = new byte[8];

                Array.Copy(bytes, offset, fee, 0, 8);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(fee);
                }

                Fee = BitConverter.ToUInt64(fee);
                offset += 8;

                TransactionKey = new Cipher.Key(new byte[32]);
                Array.Copy(bytes, offset, TransactionKey.bytes, 0, 32);
                offset += 32;

                Inputs = new TXInput[NumInputs];

                for (int i = 0; i < NumInputs; i++)
                {
                    Inputs[i] = new TXInput();

                    Inputs[i].Unmarshal(bytes, offset);
                    offset += TXInput.Size();
                }

                Outputs = new TXOutput[NumOutputs];

                for (int i = 0; i < NumOutputs; i++)
                {
                    Outputs[i] = new TXOutput();

                    Outputs[i].TXUnmarshal(bytes, offset);
                    offset += 72;
                }

                if (Version == 2)
                {
                    RangeProofPlus = new BulletproofPlus();

                    RangeProofPlus.Unmarshal(bytes, offset);
                    offset += RangeProofPlus.Size();
                }
                else
                {
                    RangeProof = new Bulletproof();

                    RangeProof.Unmarshal(bytes, offset);
                    offset += RangeProof.Size();
                }

                Signatures = new Triptych[NumSigs];

                for (int i = 0; i < NumSigs; i++)
                {
                    Signatures[i] = new Triptych();

                    Signatures[i].Unmarshal(bytes, offset);
                    offset += Triptych.Size();
                }

                PseudoOutputs = new Cipher.Key[NumInputs];
                for (int i = 0; i < NumInputs; i++)
                {
                    PseudoOutputs[i] = new Cipher.Key(new byte[32]);
                    Array.Copy(bytes, offset, PseudoOutputs[i].bytes, 0, 32);
                    offset += 32;
                }
            }
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
                Cipher.KeyOps.GenCommitment(ref tx.Outputs[i].Commitment, ref mask, amnt);
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
            Cipher.KeyOps.GenCommitment(ref tx.Outputs[0].Commitment, ref mask, amnt);
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

        public uint Unmarshal(byte[] bytes, uint offset)
        {
            if (bytes[offset] == 0)
            {
                Version = bytes[offset];
                NumInputs = bytes[offset + 1];
                NumOutputs = bytes[offset + 2];
                NumSigs = bytes[offset + 3];

                offset += 4;

                TransactionKey = new Cipher.Key(new byte[32]);
                Array.Copy(bytes, offset, TransactionKey.bytes, 0, 32);
                offset += 32;

                Outputs = new TXOutput[NumOutputs];

                for (int i = 0; i < NumOutputs; i++)
                {
                    Outputs[i] = new TXOutput();

                    Outputs[i].TXUnmarshal(bytes, offset);
                    offset += 72;
                }

                return offset;
            }
            else
            {
                Version = bytes[offset];
                NumInputs = bytes[offset + 1];
                NumOutputs = bytes[offset + 2];
                NumSigs = bytes[offset + 3];

                offset += 4;

                byte[] fee = new byte[8];

                Array.Copy(bytes, offset, fee, 0, 8);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(fee);
                }

                Fee = BitConverter.ToUInt64(fee);
                offset += 8;

                TransactionKey = new Cipher.Key(new byte[32]);
                Array.Copy(bytes, offset, TransactionKey.bytes, 0, 32);
                offset += 32;

                Inputs = new TXInput[NumInputs];

                for (int i = 0; i < NumInputs; i++)
                {
                    Inputs[i] = new TXInput();

                    Inputs[i].Unmarshal(bytes, offset);
                    offset += TXInput.Size();
                }

                Outputs = new TXOutput[NumOutputs];

                for (int i = 0; i < NumOutputs; i++)
                {
                    Outputs[i] = new TXOutput();

                    Outputs[i].TXUnmarshal(bytes, offset);
                    offset += 72;
                }

                if (Version == 2)
                {
                    RangeProofPlus = new BulletproofPlus();

                    RangeProofPlus.Unmarshal(bytes, offset);
                    offset += RangeProofPlus.Size();
                }
                else
                {
                    RangeProof = new Bulletproof();

                    RangeProof.Unmarshal(bytes, offset);
                    offset += RangeProof.Size();
                }

                Signatures = new Triptych[NumSigs];

                for (int i = 0; i < NumSigs; i++)
                {
                    Signatures[i] = new Triptych();

                    Signatures[i].Unmarshal(bytes, offset);
                    offset += Triptych.Size();
                }

                PseudoOutputs = new Cipher.Key[NumInputs];
                for (int i = 0; i < NumInputs; i++)
                {
                    PseudoOutputs[i] = new Cipher.Key(new byte[32]);
                    Array.Copy(bytes, offset, PseudoOutputs[i].bytes, 0, 32);
                    offset += 32;
                }

                return offset;
            }
        }

        public uint Size()
        {
            if (Version == 0)
            {
                return (uint)(4 + 32 + 72 * Outputs.Length);
            }
            else if (Version == 2)
            {
                return (uint)(4 + TXInput.Size() * Inputs.Length + 72 * Outputs.Length + RangeProofPlus.Size() + 8 + Triptych.Size() * Signatures.Length + 32 * PseudoOutputs.Length + 32);
            }
            return (uint)(4 + TXInput.Size() * Inputs.Length + 72 * Outputs.Length + RangeProof.Size() + 8 + Triptych.Size() * Signatures.Length + 32 * PseudoOutputs.Length + 32);
        }

        public static Transaction GenerateMock()
        {
            Transaction tx = new();
            tx.Version = 1;
            tx.NumInputs = 2;
            tx.NumOutputs = 2;
            tx.NumSigs = 2;

            tx.Inputs = new TXInput[2];
            tx.Outputs = new TXOutput[2];
            tx.Signatures = new Triptych[2];

            for (int i = 0; i < 2; i++)
            {
                tx.Inputs[i] = TXInput.GenerateMock();
                tx.Outputs[i] = TXOutput.GenerateMock();
                tx.Signatures[i] = Triptych.GenerateMock();
            }

            tx.RangeProof = Bulletproof.GenerateMock();


            var buffer = new byte[8];
            new Random().NextBytes(buffer);
            tx.Fee = BitConverter.ToUInt64(buffer, 0);
            
            tx.TransactionKey = Cipher.KeyOps.GeneratePubkey();

            return tx;
        }

        /* this function must be run PRIOR to adding a transaction to the TXPool. 
         * This is not run, and should never be run, on transactions already present in the TXPool.
         * This is definitely never used for transactions already assigned an ID in the database.
         */
        public VerifyException Verify()
        {
            DB.DB db = DB.DB.GetDB();

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
                if (Outputs[i].Amount == 0)
                {
                    return new VerifyException("Transaction", $"Amount field in output at index {i} is not set!");
                }

                if (Outputs[i].Commitment.Equals(Cipher.Key.Z))
                {
                    return new VerifyException("Transaction", $"Commitment field in output at index {i} is not set!");
                }
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
                    Cipher.KeyOps.AddKeys(ref tmp, ref sumComms, ref Outputs[i].Commitment);
                    Array.Copy(tmp.bytes, sumComms.bytes, 32);
                }

                Cipher.Key commFee = new(new byte[32]);
                Cipher.KeyOps.GenCommitment(ref commFee, ref Cipher.Key.Z, Fee);
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
                for (int i = 0; i < NumInputs; i++)
                {
                    if (Inputs[i].Offsets.Length != 64)
                    {
                        return new VerifyException("Transaction", $"Input at index {i} has an anonymity set of size {Inputs[i].Offsets.Length}; expected 64");
                    }

                    TXOutput[] mixins;
                    try
                    {
                        mixins = db.GetMixins(Inputs[i].Offsets);
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

                    if (!db.CheckSpentKey(Inputs[i].KeyImage))
                    {
                        return new VerifyException("Transaction", $"Key image for input at index {i} ({Inputs[i].KeyImage.ToHexShort()}) already spent! (double spend)");
                    }
                }
            }

            return null;
        }
    }
}
