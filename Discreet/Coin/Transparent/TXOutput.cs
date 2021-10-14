using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Discreet.Cipher;

namespace Discreet.Coin.Transparent
{
    [StructLayout(LayoutKind.Sequential)]
    public class TXOutput: ICoin
    {
        [MarshalAs(UnmanagedType.Struct)]
        SHA256 TransactionSrc;
        [MarshalAs(UnmanagedType.Struct)]
        TAddress Address;
        [MarshalAs(UnmanagedType.Struct)]
        ulong Amount;

        public TXOutput()
        {

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

            byte[] amount = BitConverter.GetBytes(Amount);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(amount);
            }

            Array.Copy(amount, 0, bytes, 57, 8);

            return bytes;
        }

        public byte[] TXMarshal()
        {
            byte[] bytes = new byte[33];

            Array.Copy(Address.Bytes(), 0, bytes, 0, 25);

            byte[] amount = BitConverter.GetBytes(Amount);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(amount);
            }

            Array.Copy(amount, 0, bytes, 25, 8);

            return bytes;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            Array.Copy(Marshal(), 0, bytes, offset, 65);
        }

        public void TXMarshal(byte[] bytes, uint offset)
        {
            Array.Copy(Marshal(), 0, bytes, offset, 33);
        }

        public string Readable()
        {
            return $"{{\"TransactionSrc\":\"{TransactionSrc.ToHex()}\",\"Address\":\"{Address}\",\"Amount\":{Amount}}}";
        }

        public string TXReadable()
        {
            return $"{{\"Address\":\"{Address}\",\"Amount\":{Amount}}}";
        }

        public void Unmarshal(byte[] bytes)
        {
            byte[] transactionSrc = new byte[32];
            Array.Copy(bytes, 0, transactionSrc, 0, 32);
            TransactionSrc = new SHA256(transactionSrc, false);

            byte[] address = new byte[25];
            Array.Copy(bytes, 32, address, 0, 25);
            Address = new TAddress(address);

            byte[] amount = new byte[8];
            Array.Copy(bytes, 57, amount, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(amount);
            }

            Amount = BitConverter.ToUInt64(amount);
        }

        public uint Unmarshal(byte[] bytes, uint offset)
        {
            byte[] transactionSrc = new byte[32];
            Array.Copy(bytes, offset, transactionSrc, 0, 32);
            TransactionSrc = new SHA256(transactionSrc, false);

            byte[] address = new byte[25];
            Array.Copy(bytes, offset + 32, address, 0, 25);
            Address = new TAddress(address);

            byte[] amount = new byte[8];
            Array.Copy(bytes, offset + 57, amount, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(amount);
            }

            Amount = BitConverter.ToUInt64(amount);

            return offset + Size();
        }

        public void TXUnmarshal(byte[] bytes)
        {
            byte[] address = new byte[25];
            Array.Copy(bytes, 0, address, 0, 25);
            Address = new TAddress(address);

            byte[] amount = new byte[8];
            Array.Copy(bytes, 25, amount, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(amount);
            }

            Amount = BitConverter.ToUInt64(amount);
        }

        public uint TXUnmarshal(byte[] bytes, uint offset)
        {
            byte[] address = new byte[25];
            Array.Copy(bytes, offset, address, 0, 25);
            Address = new TAddress(address);

            byte[] amount = new byte[8];
            Array.Copy(bytes, offset + 25, amount, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(amount);
            }

            Amount = BitConverter.ToUInt64(amount);

            return offset + 33;
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
