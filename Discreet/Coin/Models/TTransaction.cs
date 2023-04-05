using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.IO;
using Discreet.Common;
using Discreet.Common.Serialize;
using Discreet.Common.Exceptions;

namespace Discreet.Coin.Models
{
    public class TTransaction : IHashable
    {
        public byte Version { get; set; }
        public byte NumInputs { get; set; }
        public byte NumOutputs { get; set; }
        public byte NumSigs { get; set; }

        public SHA256 InnerHash { get; set; }
        public ulong Fee { get; set; }

        public TTXInput[] Inputs { get; set; }
        public TTXOutput[] Outputs { get; set; }
        public Signature[] Signatures { get; set; }

        private SHA256 _txid;
        public SHA256 TxID { get { if (_txid == default || _txid.Bytes == null) _txid = this.Hash(); return _txid; } }

        public TTransaction() { }

        public FullTransaction ToFull()
        {
            return new FullTransaction(this);
        }

        public SHA256 SigningHash()
        {
            /* Version, NumInputs, NumOutputs, Inputs, Outputs, and Fee are included. NumSigs is not since this is used to track signing progress. */
            byte[] bytes = new byte[33 * Outputs.Length + 33 * Inputs.Length + 11];
            var writer = new BEBinaryWriter(new MemoryStream(bytes));

            writer.Write(Version);
            writer.Write(NumInputs);
            writer.Write(NumOutputs);
            writer.Write(NumSigs);

            writer.WriteSerializableArray(Inputs, false);
            writer.WriteSerializableArray(Outputs, false, (x) => x.TXMarshal);
            writer.Write(Fee);

            return SHA256.HashData(bytes);
        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(NumInputs);
            writer.Write(NumOutputs);
            writer.Write(NumSigs);

            writer.Write(Fee);
            writer.WriteSHA256(InnerHash);
            writer.WriteSerializableArray(Inputs, false);
            writer.WriteSerializableArray(Outputs, false, (x) => x.TXMarshal);
            writer.WriteSerializableArray(Signatures, false);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Version = reader.ReadUInt8();
            NumInputs = reader.ReadUInt8();
            NumOutputs = reader.ReadUInt8();
            NumSigs = reader.ReadUInt8();

            Fee = reader.ReadUInt64();
            InnerHash = reader.ReadSHA256();
            Inputs = reader.ReadSerializableArray<TTXInput>(NumInputs);
            Outputs = reader.ReadSerializableArray<TTXOutput>(NumOutputs, (x) => x.TXUnmarshal);
        }

        public string Readable()
        {
            return Discreet.Readable.Transparent.Transaction.ToReadable(this);
        }

        public object ToReadable()
        {
            return new Readable.Transparent.Transaction(this);
        }

        public static TTransaction FromReadable(string json)
        {
            return Discreet.Readable.Transparent.Transaction.FromReadable(json);
        }

        public uint GetSize()
        {
            return (uint)(44 + TTXInput.GetSize() * Inputs.Length + 33 * Outputs.Length + 96 * Signatures.Length);
        }

        public int Size => (int)GetSize();

        private VerifyException verify(bool signed, bool inBlock = false)
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

            HashSet<TTXInput> _in = new HashSet<TTXInput>(new Comparers.TTXInputEqualityComparer());
            TTXOutput[] inputValues = new TTXOutput[Inputs.Length];

            var dataView = DB.DataView.GetView();
            var pool = Daemon.TXPool.GetTXPool();

            for (int i = 0; i < NumInputs; i++)
            {
                _in.Add(Inputs[i]);

                try
                {
                    inputValues[i] = DB.DataView.GetView().GetPubOutput(Inputs[i]);
                }
                catch (Exception e)
                {
                    Daemon.Logger.Error("Transparent.Transaction.Verify: " + e.Message, e);
                    return new VerifyException("Transparent.Transaction", $"Input at index {i} for transaction not present in UTXO set!");
                }
            }

            if (_in.Count != NumInputs)
            {
                return new VerifyException("Transparent.Transaction", $"Duplicate inputs detected!");
            }

            for (int i = 0; i < Inputs.Length; i++)
            {
                var aexc = inputValues[i].Address.Verify();
                if (aexc != null) return aexc;

                if (!inputValues[i].Address.CheckAddressBytes(Signatures[i].y))
                {
                    return new VerifyException("Transparent.Transaction", $"Input at index {i}'s address ({inputValues[i].Address}) does not match public key in signature ({Signatures[i].y.ToHexShort()})");
                }

                /* check if present in database */


                /* check if tx is in pool */
                if (!inBlock)
                {
                    if (pool.ContainsSpent(Inputs[i]))
                    {
                        return new VerifyException("Transparent.Transaction", $"Input at index {i} was spent in a previous transaction currently in the mempool");
                    }
                }
            }

            if (Version != (byte)Config.TransactionVersions.TRANSPARENT)
            {
                return new VerifyException("Transparent.Transaction", $"Invalid transaction version: expected {(byte)Config.TransactionVersions.TRANSPARENT}, but got {Version}");
            }

            foreach (TTXOutput output in Outputs)
            {
                if (output.Amount == 0)
                {
                    return new VerifyException("Transparent.Transaction", "zero coins in output!");
                }
            }

            ulong _amount = 0;

            foreach (TTXOutput output in Outputs)
            {
                try
                {
                    _amount = checked(_amount + output.Amount);
                }
                catch (OverflowException)
                {
                    return new VerifyException("Transparent.Transaction", $"transaction resulted in overflow!");
                }
            }

            try
            {
                _amount = checked(_amount + Fee);
            }
            catch (OverflowException)
            {
                return new VerifyException("Transparent.Transaction", $"transaction fee resulted in overflow!");
            }

            ulong inAmount = 0;
            foreach (TTXOutput input in inputValues)
            {
                inAmount += input.Amount;
            }

            if (inAmount != _amount)
            {
                return new VerifyException("Transparent.Transaction", $"Transaction does not balance! {inAmount} (sum of inputs) != {_amount} (sum of outputs)");
            }

            SHA256 txhash = this.Hash();

            HashSet<SHA256> _out = new HashSet<SHA256>();

            for (int i = 0; i < NumOutputs; i++)
            {
                _out.Add(new TTXOutput(txhash, Outputs[i].Address, Outputs[i].Amount).Hash());
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
                Array.Copy(Inputs[i].Hash(inputValues[i]).Bytes, 0, data, 32, 32);

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

        public VerifyException Verify(bool inBlock = false)
        {
            return verify(NumSigs == NumInputs, inBlock: inBlock);
        }

        public VerifyException Verify()
        {
            return Verify(inBlock: false);
        }
    }
}
