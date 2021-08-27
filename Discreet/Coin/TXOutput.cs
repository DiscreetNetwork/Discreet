using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.Text.Json;

namespace Discreet.Coin
{
    /**
     * WIP
     * 
     * Transaction outputs come in both transparent and private varieties.
     * 
     * 
     * 
     * 
     * 
     */

    [StructLayout(LayoutKind.Sequential)]
    public class TxOutputHeader: ICoin
    {
        [MarshalAs(UnmanagedType.U8)]
        public ulong Timestamp;
        [MarshalAs(UnmanagedType.U8)]
        public ulong BlockNumber;

        public TxOutputHeader()
        {
            Timestamp = 0;
            BlockNumber = 0;
        }

        public TxOutputHeader(ulong timestamp, ulong blockNumber)
        {
            Timestamp = timestamp;
            BlockNumber = blockNumber;
        }

        public SHA256 Hash()
        {
            return SHA256.HashData(Marshal());
        }

        public byte[] Marshal()
        {
            byte[] time = BitConverter.GetBytes(Timestamp);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(time);
            }

            byte[] blocknumber = BitConverter.GetBytes(BlockNumber);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(blocknumber);
            }

            byte[] rv = new byte[Size()];

            Array.Copy(time, 0, rv, 0, 8);
            Array.Copy(blocknumber, 0, rv, 8, 8);

            return rv;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            byte[] rv = Marshal();

            Array.Copy(bytes, offset, rv, 0, Size());
        }

        public string Readable()
        {
            return JsonSerializer.Serialize(this);
        }

        public static uint Size()
        {
            return 16;
        }

        public void Unmarshal(byte[] bytes)
        {
            byte[] time = new byte[8];
            byte[] blocknumber = new byte[8];

            Array.Copy(bytes, 0, time, 0, 8);
            Array.Copy(bytes, 8, blocknumber, 0, 8);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(time);
                Array.Reverse(blocknumber);
            }

            Timestamp = BitConverter.ToUInt64(time);
            BlockNumber = BitConverter.ToUInt64(blocknumber);
        }

        public void Unmarshal(byte[] bytes, uint offset)
        {
            byte[] time = new byte[8];
            byte[] blocknumber = new byte[8];

            Array.Copy(bytes, offset, time, 0, 8);
            Array.Copy(bytes, offset + 8, blocknumber, 0, 8);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(time);
                Array.Reverse(blocknumber);
            }

            Timestamp = BitConverter.ToUInt64(time);
            BlockNumber = BitConverter.ToUInt64(blocknumber);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class PTxOutputBody: ICoin
    {
        [MarshalAs(UnmanagedType.Struct)]
        Discreet.Cipher.SHA256 TransactionSrc;
        [MarshalAs(UnmanagedType.Struct)]
        Discreet.Cipher.Key UXKey;
        [MarshalAs(UnmanagedType.Struct)]
        Discreet.Cipher.Key Value;

        public PTxOutputBody()
        {
            TransactionSrc = new SHA256(new byte[32], false);
            UXKey = new Key(new byte[32]);
            Value = new Key(new byte[32]);
        }

        public PTxOutputBody(SHA256 transactionSrc, Key uxKey, Key value)
        {
            TransactionSrc = transactionSrc;
            UXKey = uxKey;
            Value = value;
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
            Array.Copy(Value.bytes, 0, rv, 64, 32);

            return rv;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            byte[] rv = Marshal();

            Array.Copy(bytes, offset, rv, 0, Size());
        }

        public string Readable()
        {
            return $"{{\"TransactionSrc\": \"{TransactionSrc.ToHex()}\",\"UXKey\":\"{UXKey.ToHex()}\",\"Value\":\"{Value.ToHex()}\"}}";
        }

        public static uint Size()
        {
            return 96;
        }

        public void Unmarshal(byte[] bytes)
        {
            byte[] transactionSrc = new byte[32];
            Array.Copy(bytes, 0, transactionSrc, 0, 32);
            TransactionSrc = new SHA256(transactionSrc, false);

            Array.Copy(bytes, 32, UXKey.bytes, 0, 32);
            Array.Copy(bytes, 64, Value.bytes, 0, 32);
        }

        public void Unmarshal(byte[] bytes, uint offset)
        {
            byte[] transactionSrc = new byte[32];
            Array.Copy(bytes, offset, transactionSrc, 0, 32);
            TransactionSrc = new SHA256(transactionSrc, false);

            Array.Copy(bytes, offset + 32, UXKey.bytes, 0, 32);
            Array.Copy(bytes, offset + 64, Value.bytes, 0, 32);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class TTxOutputBody: ICoin
    {
        [MarshalAs(UnmanagedType.Struct)]
        Discreet.Cipher.SHA256 TransactionSrc;
        [MarshalAs(UnmanagedType.Struct)]
        TAddress Address;
        [MarshalAs(UnmanagedType.Struct)]
        ulong Value;

        public TTxOutputBody()
        {
            TransactionSrc = new SHA256(new byte[32], false);
            Address = new TAddress(new byte[25]);
            Value = 0;
        }

        public TTxOutputBody(SHA256 transactionSrc, TAddress address, ulong value)
        {
            TransactionSrc = transactionSrc;
            Address = address;
            Value = value;
        }

        public SHA256 Hash()
        {
            return SHA256.HashData(Marshal());
        }

        public byte[] Marshal()
        {
            byte[] value = BitConverter.GetBytes(Value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(value);
            }

            byte[] rv = new byte[Size()];

            Array.Copy(TransactionSrc.Bytes, 0, rv, 0, 32);
            Array.Copy(Address.Bytes(), 0, rv, 32, 25);
            Array.Copy(value, 0, rv, 57, 8);

            return rv;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            byte[] rv = Marshal();

            Array.Copy(bytes, offset, rv, 0, Size());
        }

        public string Readable()
        {
            return $"{{\"TransactionSrc\":\"{TransactionSrc.ToHex()}\",\"Address\":\"{Address.String()}\",\"Value\":\"{Value}\"}}";
        }

        public static uint Size()
        {
            return 65;
        }

        public void Unmarshal(byte[] bytes)
        {
            byte[] value = new byte[8];
            Array.Copy(bytes, 57, value, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(value);
            }
            
            Value = BitConverter.ToUInt64(value);

            byte[] transactionSrc = new byte[32];
            Array.Copy(bytes, 0, transactionSrc, 0, 32);
            TransactionSrc = new SHA256(transactionSrc, false);

            byte[] address = new byte[25];
            Array.Copy(bytes, 32, address, 0, 25);
            Address.FromBytes(address);
        }

        public void Unmarshal(byte[] bytes, uint offset)
        {
            byte[] value = new byte[8];
            Array.Copy(bytes, offset + 57, value, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(value);
            }

            Value = BitConverter.ToUInt64(value);

            byte[] transactionSrc = new byte[32];
            Array.Copy(bytes, offset, transactionSrc, 0, 32);
            TransactionSrc = new SHA256(transactionSrc, false);

            byte[] address = new byte[25];
            Array.Copy(bytes, offset + 32, address, 0, 25);
            Address.FromBytes(address);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class TTXOutput: ICoin
    {
        [MarshalAs(UnmanagedType.Struct)]
        TxOutputHeader Head;
        [MarshalAs(UnmanagedType.Struct)]
        TTxOutputBody Body;

        public TTXOutput()
        {
            Head = new TxOutputHeader();
            Body = new TTxOutputBody();
        }

        public TTXOutput(TxOutputHeader header, TTxOutputBody body)
        {
            Head = header;
            Body = body;
        }

        public SHA256 Hash()
        {
            throw new NotImplementedException();
        }

        public byte[] Marshal()
        {
            byte[] rv = new byte[TxOutputHeader.Size() + TTxOutputBody.Size()];

            Array.Copy(Head.Marshal(), 0, rv, 0, TxOutputHeader.Size());
            Array.Copy(Body.Marshal(), 0, rv, TxOutputHeader.Size(), TTxOutputBody.Size());

            return rv;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            byte[] rv = Marshal();

            Array.Copy(bytes, offset, rv, 0, Size());
        }

        public string Readable()
        {
            return $"{{\"Head\":{Head.Readable()},\"Body\":{Body.Readable()}}}";
        }

        public static uint Size()
        {
            return TxOutputHeader.Size() + TTxOutputBody.Size();
        }

        public void Unmarshal(byte[] bytes)
        {
            Head.Unmarshal(bytes, 0);
            Body.Unmarshal(bytes, TxOutputHeader.Size());
        }

        public void Unmarshal(byte[] bytes, uint offset)
        {
            Head.Unmarshal(bytes, offset);
            Body.Unmarshal(bytes, offset + TxOutputHeader.Size());
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class PTXOutput: ICoin
    {
        [MarshalAs(UnmanagedType.Struct)]
        TxOutputHeader Head;
        [MarshalAs(UnmanagedType.Struct)]
        PTxOutputBody Body;

        public PTXOutput()
        {
            Head = new TxOutputHeader();
            Body = new PTxOutputBody();
        }

        public PTXOutput(TxOutputHeader header, PTxOutputBody body)
        {
            Head = header;
            Body = body;
        }

        public SHA256 Hash()
        {
            throw new NotImplementedException();
        }

        public byte[] Marshal()
        {
            byte[] rv = new byte[TxOutputHeader.Size() + PTxOutputBody.Size()];

            Array.Copy(Head.Marshal(), 0, rv, 0, TxOutputHeader.Size());
            Array.Copy(Body.Marshal(), 0, rv, TxOutputHeader.Size(), PTxOutputBody.Size());

            return rv;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            byte[] rv = Marshal();

            Array.Copy(bytes, offset, rv, 0, Size());
        }

        public string Readable()
        {
            return $"{{\"Head\":{Head.Readable()},\"Body\":{Body.Readable()}}}";
        }

        public static uint Size()
        {
            return TxOutputHeader.Size() + PTxOutputBody.Size();
        }

        public void Unmarshal(byte[] bytes)
        {
            Head.Unmarshal(bytes, 0);
            Body.Unmarshal(bytes, TxOutputHeader.Size());
        }

        public void Unmarshal(byte[] bytes, uint offset)
        {
            Head.Unmarshal(bytes, offset);
            Body.Unmarshal(bytes, offset + TxOutputHeader.Size());
        }
    }
}
