using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.Text.Json.Serialization;
using System.IO;

namespace Discreet.Coin
{
    [StructLayout(LayoutKind.Sequential)]
    public class TXOutput : ICoin
    {
        [MarshalAs(UnmanagedType.Struct), JsonIgnore]
        public Discreet.Cipher.SHA256 TransactionSrc;
        [MarshalAs(UnmanagedType.Struct)]
        public Discreet.Cipher.Key UXKey;
        [MarshalAs(UnmanagedType.Struct)]
        public Discreet.Cipher.Key Commitment;
        [MarshalAs(UnmanagedType.U8)]
        public ulong Amount;

        [MarshalAs(UnmanagedType.U4), JsonIgnore]
        public uint Index; /* unused mostly except for CreateTransaction() */

        public TXOutput()
        {
            TransactionSrc = new SHA256(new byte[32], false);
            UXKey = new Key(new byte[32]);
            Commitment = new Key(new byte[32]);
            Amount = 0;
        }

        public TXOutput(SHA256 transactionSrc, Key uxKey, Key commitment, ulong amount)
        {
            TransactionSrc = transactionSrc;
            UXKey = uxKey;
            Commitment = commitment;
            Amount = amount;
        }

        public SHA256 Hash()
        {
            return SHA256.HashData(Serialize());
        }

        public byte[] Serialize()
        {
            byte[] rv = new byte[Size()];

            Array.Copy(TransactionSrc.Bytes, 0, rv, 0, 32);
            Array.Copy(UXKey.bytes, 0, rv, 32, 32);
            Array.Copy(Commitment.bytes, 0, rv, 64, 32);
            Serialization.CopyData(rv, 96, Amount);

            return rv;
        }

        public void Serialize(byte[] bytes, uint offset)
        {
            byte[] rv = Serialize();
            Array.Copy(bytes, offset, rv, 0, Size());
        }

        public byte[] TXMarshal()
        {
            byte[] rv = new byte[72];

            Array.Copy(UXKey.bytes, 0, rv, 0, 32);
            Array.Copy(Commitment.bytes, 0, rv, 32, 32);
            Serialization.CopyData(rv, 64, Amount);

            return rv;
        }

        public void TXMarshal(byte[] bytes, uint offset)
        {
            byte[] rv = TXMarshal();

            Array.Copy(rv, 0, bytes, offset, 72);
        }

        public string Readable()
        {
            return Discreet.Readable.TXOutput.ToReadable(this);
        }

        public string TXReadable()
        {
            return Discreet.Readable.TXOutput.ToTXReadable(this);
        }

        public object ToReadable()
        {
            return new Discreet.Readable.TXOutput(this);
        }

        public object ToTXReadable()
        {
            return new Discreet.Readable.TXOutput(this, true);
        }

        public static TXOutput FromReadable(string json)
        {
            return Discreet.Readable.TXOutput.FromReadable(json);
        }

        public static uint Size()
        {
            return 104;
        }

        public void Deserialize(byte[] bytes)
        {
            Deserialize(bytes, 0);
        }

        public uint Deserialize(byte[] bytes, uint offset)
        {
            TransactionSrc = new SHA256(bytes, offset);
            UXKey = new Key(bytes, offset + 32);
            Commitment = new Key(bytes, offset + 64);
            Amount = Serialization.GetUInt64(bytes, offset + 96);

            return offset + 104;
        }

        public void TXUnmarshal(byte[] bytes)
        {
            TXUnmarshal(bytes, 0);
        }

        public uint TXUnmarshal(byte[] bytes, uint offset)
        {
            UXKey = new Key(bytes, offset);
            Commitment = new Key(bytes, offset + 32);
            Amount = Serialization.GetUInt64(bytes, offset + 64);

            return offset + 72;
        }

        public void Serialize(Stream s)
        {
            s.Write(TransactionSrc.Bytes);
            s.Write(UXKey.bytes);
            s.Write(Commitment.bytes);
            Serialization.CopyData(s, Amount);
        }

        public void Deserialize(Stream s)
        {
            TransactionSrc = new SHA256(s);
            UXKey = new Key(s);
            Commitment = new Key(s);
            Amount = Serialization.GetUInt64(s);
        }

        public void TXMarshal(Stream s)
        {
            s.Write(UXKey.bytes);
            s.Write(Commitment.bytes);
            Serialization.CopyData(s, Amount);
        }

        public void TXUnmarshal(Stream s)
        {
            UXKey = new Key(s);
            Commitment = new Key(s);
            Amount = Serialization.GetUInt64(s);
        }

        public static TXOutput GenerateMock()
        {
            TXOutput output = new();
            Random rng = new();

            ulong v1 = ((ulong)rng.Next(0, Int32.MaxValue)) << 32;
            ulong v2 = (ulong)rng.Next(0, int.MaxValue);
            output.Amount = v1 | v2;

            output.UXKey = Cipher.KeyOps.GeneratePubkey();
            output.Commitment = Cipher.KeyOps.GeneratePubkey();
            output.TransactionSrc = new Cipher.SHA256(new byte[32], false);

            return output;
        }

        public VerifyException Verify()
        {
            return null;
        }
    }
}
