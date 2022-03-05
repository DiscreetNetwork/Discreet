using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.IO;

namespace Discreet.Coin.Transparent
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
        public SHA256 InnerHash;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public TXOutput[] Inputs;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public TXOutput[] Outputs;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public Signature[] Signatures;

        public SHA256 Hash()
        {
            return SHA256.HashData(Serialize());
        }

        public Transaction() { }

        public FullTransaction ToFull()
        {
            return new FullTransaction(this);
        }

        public SHA256 SigningHash()
        {
            /* Version, NumInputs, NumOutputs, Inputs, Outputs, and Fee are included. NumSigs is not since this is used to track signing progress. */
            byte[] bytes = new byte[33 * Outputs.Length + TXOutput.Size() * Inputs.Length + 11];

            bytes[0] = Version;
            bytes[1] = NumInputs;
            bytes[2] = NumOutputs;

            uint offset = 3;

            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i].Serialize(bytes, offset);
                offset += TXOutput.Size();
            }

            for (int i = 0; i < Outputs.Length; i++)
            {
                Outputs[i].TXMarshal(bytes, offset);
                offset += 33;
            }

            Serialization.CopyData(bytes, offset, Fee);

            return SHA256.HashData(bytes);
        }

        public byte[] Serialize()
        {
            byte[] bytes = new byte[Size()];

            bytes[0] = Version;
            bytes[1] = NumInputs;
            bytes[2] = NumOutputs;
            bytes[3] = NumSigs;

            uint offset = 4;

            Serialization.CopyData(bytes, offset, Fee);
            offset += 8;

            Array.Copy(InnerHash.Bytes, 0, bytes, offset, 32);
            offset += 32;

            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i].Serialize(bytes, offset);
                offset += 65;
            }

            for (int i = 0; i < Outputs.Length; i++)
            {
                Outputs[i].TXMarshal(bytes, offset);
                offset += 33;
            }

            for (int i = 0; i < Signatures.Length; i++)
            {
                Signatures[i].ToBytes(bytes, offset);
                offset += 96;
            }

            return bytes;
        }

        public void Serialize(byte[] bytes, uint offset)
        {
            byte[] rv = Serialize();

            Array.Copy(rv, 0, bytes, offset, rv.Length);
        }

        public string Readable()
        {
            return Discreet.Readable.Transparent.Transaction.ToReadable(this);
        }

        public object ToReadable()
        {
            return new Discreet.Readable.Transparent.Transaction(this);
        }

        public static Transaction FromReadable(string json)
        {
            return Discreet.Readable.Transparent.Transaction.FromReadable(json);
        }

        public void Deserialize(byte[] bytes)
        {
            Deserialize(bytes, 0);
        }

        public uint Deserialize(byte[] bytes, uint offset)
        {
            Version = bytes[offset];
            NumInputs = bytes[offset + 1];
            NumOutputs = bytes[offset + 2];
            NumSigs = bytes[offset + 3];

            offset += 4;

            Fee = Serialization.GetUInt64(bytes, offset);
            offset += 8;

            InnerHash = new SHA256(bytes, offset);
            offset += 32;

            Inputs = new TXOutput[NumInputs];

            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i] = new TXOutput();
                Inputs[i].Deserialize(bytes, offset);
                offset += TXOutput.Size();
            }

            Outputs = new TXOutput[NumOutputs];

            for (int i = 0; i < Outputs.Length; i++)
            {
                Outputs[i] = new TXOutput();
                Outputs[i].TXUnmarshal(bytes, offset);
                offset += 33;
            }

            Signatures = new Signature[NumSigs];

            for (int i = 0; i < Signatures.Length; i++)
            {
                Signatures[i] = new Signature(bytes, offset);
                offset += 96;
            }

            return offset;
        }

        public void Serialize(Stream s)
        {
            s.WriteByte(Version);
            s.WriteByte(NumInputs);
            s.WriteByte(NumOutputs);
            s.WriteByte(NumSigs);

            Serialization.CopyData(s, Fee);

            s.Write(InnerHash.Bytes);

            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i].Serialize(s);
            }

            for (int i = 0; i < Outputs.Length; i++)
            {
                Outputs[i].TXMarshal(s);
            }

            for (int i = 0; i < Signatures.Length; i++)
            {
                s.Write(Signatures[i].ToBytes());
            }
        }

        public void Deserialize(Stream s)
        {
            Version = (byte)s.ReadByte();
            NumInputs = (byte)s.ReadByte();
            NumOutputs = (byte)s.ReadByte();
            NumSigs = (byte)s.ReadByte();

            Fee = Serialization.GetUInt64(s);

            InnerHash = new SHA256(s);

            Inputs = new TXOutput[NumInputs];
            Outputs = new TXOutput[NumOutputs];
            Signatures = new Signature[NumSigs];

            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i] = new TXOutput();
                Inputs[i].Deserialize(s);
            }

            for (int i = 0; i < Outputs.Length; i++)
            {
                Outputs[i] = new TXOutput();
                Outputs[i].TXUnmarshal(s);
            }

            for (int i = 0; i < Signatures.Length; i++)
            {
                Signatures[i] = new Signature(s);
            }
        }

        public uint Size()
        {
            return (uint)(44 + TXOutput.Size() * Inputs.Length + 33 * Outputs.Length + 96 * Signatures.Length);
        }

        private VerifyException verify(bool signed)
        {
            if (Inputs == null || Inputs.Length == 0 || NumInputs == 0)
            {
                return new VerifyException("Transparent.Transaction", "Zero inputs");
            }

            if (Outputs == null || Outputs.Length == 0 || NumOutputs == 0)
            {
                return new VerifyException("Transparent.Transaction", "Zero outputs");
            }

            if (Outputs.Length != NumOutputs)
            {
                return new VerifyException("Transparent.Transaction", $"Output number mismatch: expected {NumOutputs}, but got {Outputs.Length}");
            }

            if (Inputs.Length != NumInputs)
            {
                return new VerifyException("Transparent.Transaction", $"Input number mismatch: expected {NumInputs}, but got {Inputs.Length}");
            }

            if (NumInputs > Config.TRANSPARENT_MAX_NUM_INPUTS)
            {
                return new VerifyException("Transparent.Transaction", $"Number of Inputs exceeds the maximum of {Config.TRANSPARENT_MAX_NUM_INPUTS} ({NumInputs} Inputs present)");
            }

            if (NumOutputs > Config.TRANSPARENT_MAX_NUM_OUTPUTS)
            {
                return new VerifyException("Transparent.Transaction", $"Number of Inputs exceeds the maximum of {Config.TRANSPARENT_MAX_NUM_OUTPUTS} ({NumOutputs} Inputs present)");
            }

            if (NumSigs > NumInputs)
            {
                return new VerifyException("Transparent.Transaction", $"Invalid number of signatures: {NumSigs} > {NumInputs}, exceeding the number of inputs");
            }

            HashSet<SHA256> _in = new HashSet<SHA256>();

            for (int i = 0; i < NumInputs; i++)
            {
                _in.Add(Inputs[i].Hash());
            }

            if (_in.Count != NumInputs)
            {
                return new VerifyException("Transparent.Transaction", $"Duplicate inputs detected!");
            }

            if (Version != (byte)Config.TransactionVersions.TRANSPARENT)
            {
                return new VerifyException("Transparent.Transaction", $"Invalid transaction version: expected {(byte)Config.TransactionVersions.TRANSPARENT}, but got {Version}");
            }

            foreach (TXOutput output in Outputs)
            {
                if (output.Amount == 0)
                {
                    return new VerifyException("Transparent.Transaction", "zero coins in output!");
                }
            }

            ulong _amount = 0;

            foreach(TXOutput output in Outputs)
            {
                try
                {
                    _amount = checked(_amount + output.Amount);
                }
                catch(OverflowException)
                {
                    return new VerifyException("Transparent.Transaction", $"transaction resulted in overflow!");
                }
            }

            try
            {
                _amount = checked(_amount + Fee);
            }
            catch(OverflowException)
            {
                return new VerifyException("Transparent.Transaction", $"transaction fee resulted in overflow!");
            }

            ulong inAmount = 0;
            foreach(TXOutput input in Inputs)
            {
                inAmount += input.Amount;
            }

            if (inAmount != _amount)
            {
                return new VerifyException("Transparent.Transaction", $"Transaction does not balance! {inAmount} (sum of inputs) != {_amount} (sum of outputs)");
            }

            SHA256 txhash = Hash();

            HashSet<SHA256> _out = new HashSet<SHA256>();

            for (int i = 0; i < NumOutputs; i++)
            {
                _out.Add(new TXOutput(txhash, Outputs[i].Address, Outputs[i].Amount).Hash());
            }

            if (_out.Count != NumOutputs)
            {
                return new VerifyException("Transparent.Transaction", $"Duplicate outputs detected!");
            }

            if (InnerHash != SigningHash())
            {
                return new VerifyException("Transparent.Transaction", $"InnerHash {InnerHash.ToHexShort()} does not match computed inner hash {SigningHash().ToHexShort()}");
            }

            if (Signatures.Length != NumInputs)
            {
                return new VerifyException("Transparent.Transaction", $"Signature number mismatch: expected {NumInputs}, but got {Signatures.Length}");
            }

            bool hasNullSig = false;

            for (int i = 0; i < Signatures.Length; i++)
            {
                if (Signatures[i].IsNull())
                {
                    if (signed)
                    {
                        return new VerifyException("Transparent.Transaction", $"Unsigned input present in transaction!");
                    }

                    hasNullSig = true;

                    continue;
                }

                byte[] data = new byte[64];
                Array.Copy(InnerHash.Bytes, data, 32);
                Array.Copy(Inputs[i].Hash().Bytes, 0, data, 32, 32);

                SHA256 checkSig = SHA256.HashData(data);

                if (!Signatures[i].Verify(checkSig))
                {
                    return new VerifyException("Transparent.Transaction", $"Signature failed verification!");
                }
            }

            if (!signed && !hasNullSig)
            {
                return new VerifyException("Transparent.Transaction", $"Partially/fully unsigned transaction must contain at least one null signature!");
            }

            return null;
        }

        public VerifyException Verify()
        {
            return verify(NumSigs == NumInputs);
        }
    }
}
