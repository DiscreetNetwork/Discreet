using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;

namespace Discreet.Coin
{
    [StructLayout(LayoutKind.Sequential)]
    public class TXOutput : ICoin
    {
        [MarshalAs(UnmanagedType.Struct)]
        Discreet.Cipher.SHA256 TransactionSrc;
        [MarshalAs(UnmanagedType.Struct)]
        Discreet.Cipher.Key UXKey;
        [MarshalAs(UnmanagedType.Struct)]
        Discreet.Cipher.Key Commitment;
        [MarshalAs(UnmanagedType.U8)]
        ulong Amount;

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
            return SHA256.HashData(Marshal());
        }

        public byte[] Marshal()
        {
            byte[] rv = new byte[Size()];

            Array.Copy(TransactionSrc.Bytes, 0, rv, 0, 32);
            Array.Copy(UXKey.bytes, 0, rv, 32, 32);
            Array.Copy(Commitment.bytes, 0, rv, 64, 32);

            byte[] amount = BitConverter.GetBytes(Amount);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(amount);
            }

            Array.Copy(amount, 0, rv, 96, 8);

            return rv;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            byte[] rv = Marshal();

            Array.Copy(bytes, offset, rv, 0, Size());
        }

        public byte[] TXMarshal()
        {
            byte[] rv = new byte[72];

            Array.Copy(UXKey.bytes, 0, rv, 0, 32);
            Array.Copy(Commitment.bytes, 0, rv, 32, 32);

            byte[] amount = BitConverter.GetBytes(Amount);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(amount);
            }

            Array.Copy(amount, 0, rv, 64, 8);

            return rv;
        }

        public void TXMarshal(byte[] bytes, uint offset)
        {
            byte[] rv = TXMarshal();

            Array.Copy(bytes, offset, rv, 0, Size());
        }

        public string Readable()
        {
            return $"{{\"TransactionSrc\": \"{TransactionSrc.ToHex()}\",\"UXKey\":\"{UXKey.ToHex()}\",\"Commitment\":\"{Commitment.ToHex()}\",\"Amount\":\"{Amount:X}\"}}";
        }

        public string TXReadable()
        {
            return $"{{\"UXKey\":\"{UXKey.ToHex()}\",\"Commitment\":\"{Commitment.ToHex()}\",\"Amount\":\"{Amount:X}\"}}";
        }

        public static uint Size()
        {
            return 104;
        }

        public void Unmarshal(byte[] bytes)
        {
            byte[] transactionSrc = new byte[32];
            Array.Copy(bytes, 0, transactionSrc, 0, 32);
            TransactionSrc = new SHA256(transactionSrc, false);

            Array.Copy(bytes, 32, UXKey.bytes, 0, 32);
            Array.Copy(bytes, 64, Commitment.bytes, 0, 32);

            byte[] amount = new byte[8];
            Array.Copy(bytes, 96, amount, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(amount);
            }

            Amount = BitConverter.ToUInt64(amount);
        }

        public void Unmarshal(byte[] bytes, uint offset)
        {
            byte[] transactionSrc = new byte[32];
            Array.Copy(bytes, offset, transactionSrc, 0, 32);
            TransactionSrc = new SHA256(transactionSrc, false);

            Array.Copy(bytes, offset + 32, UXKey.bytes, 0, 32);
            Array.Copy(bytes, offset + 64, Commitment.bytes, 0, 32);

            byte[] amount = new byte[8];
            Array.Copy(bytes, offset + 96, amount, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(amount);
            }

            Amount = BitConverter.ToUInt64(amount);
        }

        public void TXUnmarshal(byte[] bytes)
        {
            Array.Copy(bytes, 0, UXKey.bytes, 0, 32);
            Array.Copy(bytes, 0, Commitment.bytes, 0, 32);

            byte[] amount = new byte[8];
            Array.Copy(bytes, 64, amount, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(amount);
            }

            Amount = BitConverter.ToUInt64(amount);
        }

        public void TXUnmarshal(byte[] bytes, uint offset)
        {
            Array.Copy(bytes, offset, UXKey.bytes, 0, 32);
            Array.Copy(bytes, offset + 32, Commitment.bytes, 0, 32);

            byte[] amount = new byte[8];
            Array.Copy(bytes, offset + 64, amount, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(amount);
            }

            Amount = BitConverter.ToUInt64(amount);
        }
    }
}
