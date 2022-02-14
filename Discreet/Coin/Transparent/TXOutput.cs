using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.IO;

namespace Discreet.Coin.Transparent
{
    [StructLayout(LayoutKind.Sequential)]
    public class TXOutput: ICoin
    {
        [MarshalAs(UnmanagedType.Struct)]
        public SHA256 TransactionSrc;
        [MarshalAs(UnmanagedType.Struct)]
        public TAddress Address;
        [MarshalAs(UnmanagedType.Struct)]
        public ulong Amount;

        public TXOutput()
        {

        }

        public TXOutput(SHA256 transactionSrc, TAddress address, ulong amount)
        {
            TransactionSrc = transactionSrc;
            Address = address;
            Amount = amount;
        }

        public SHA256 Hash()
        {
            return new SHA256(Marshal(), true);
        }

        public byte[] Marshal()
        {
            byte[] bytes = new byte[65];

            Array.Copy(TransactionSrc.Bytes, bytes, 32);
            Array.Copy(Address.Bytes(), 0, bytes, 32, 25);
            Serialization.CopyData(bytes, 57, Amount);

            return bytes;
        }

        public byte[] TXMarshal()
        {
            byte[] bytes = new byte[33];

            Array.Copy(Address.Bytes(), 0, bytes, 0, 25);
            Serialization.CopyData(bytes, 25, Amount);

            return bytes;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            Array.Copy(Marshal(), 0, bytes, offset, 65);
        }

        public void TXMarshal(byte[] bytes, uint offset)
        {
            Array.Copy(TXMarshal(), 0, bytes, offset, 33);
        }

        public string Readable()
        {
            return Discreet.Readable.Transparent.TXOutput.ToReadable(this);
        }

        public string TXReadable()
        {
            return Discreet.Readable.Transparent.TXOutput.ToTXReadable(this);
        }

        public static TXOutput FromReadable(string json)
        {
            return Discreet.Readable.Transparent.TXOutput.FromReadable(json);
        }

        public void Unmarshal(byte[] bytes)
        {
            Unmarshal(bytes, 0);
        }

        public uint Unmarshal(byte[] bytes, uint offset)
        {
            TransactionSrc = new SHA256(bytes, offset);
            Address = new TAddress(bytes, offset + 32);
            Amount = Serialization.GetUInt64(bytes, offset + 57);

            return offset + Size();
        }

        public void TXUnmarshal(byte[] bytes)
        {
            TXUnmarshal(bytes, 0);
        }

        public uint TXUnmarshal(byte[] bytes, uint offset)
        {
            Address = new TAddress(bytes, offset);
            Amount = Serialization.GetUInt64(bytes, offset + 25);

            return offset + 33;
        }

        public void Marshal(Stream s)
        {
            s.Write(TransactionSrc.Bytes);
            s.Write(Address.Bytes());
            s.Write(Serialization.UInt64(Amount));
        }

        public void TXMarshal(Stream s)
        {
            s.Write(Address.Bytes());
            s.Write(Serialization.UInt64(Amount));
        }

        public void Unmarshal(Stream s)
        {
            TransactionSrc = new SHA256(s);
            Address = new TAddress(s);
            Amount = Serialization.GetUInt64(s);
        }

        public void TXUnmarshal(Stream s)
        {
            Address = new TAddress(s);
            Amount = Serialization.GetUInt64(s);
        }

        public static uint Size()
        {
            return 33 + 32;
        }

        public VerifyException Verify()
        {
            return null;
        }
    }
}
