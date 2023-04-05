using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.IO;
using Discreet.Common;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;
using System.Drawing;
using Microsoft.VisualBasic;

namespace Discreet.Coin.Models
{
    public class TTXOutput : IHashable
    {
        public SHA256 TransactionSrc { get; set; }
        public TAddress Address { get; set; }
        public ulong Amount { get; set; }

        public TTXOutput()
        {

        }

        public TTXOutput(SHA256 transactionSrc, TAddress address, ulong amount)
        {
            TransactionSrc = transactionSrc;
            Address = address;
            Amount = amount;
        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteSHA256(TransactionSrc);
            writer.WriteByteArray(Address.Bytes(), false);
            writer.Write(Amount);
        }

        public void TXMarshal(BEBinaryWriter writer)
        {
            writer.WriteByteArray(Address.Bytes(), false);
            writer.Write(Amount);
        }

        public void TXMarshal(byte[] bytes, int offset = 0) => TXMarshal(new BEBinaryWriter(new MemoryStream(bytes, offset, TXSize)));

        public byte[] TXMarshal()
        {
            byte[] rv = new byte[TXSize];
            TXMarshal(rv);
            return rv;
        }

        public string Readable()
        {
            return Discreet.Readable.Transparent.TXOutput.ToReadable(this);
        }

        public string TXReadable()
        {
            return Discreet.Readable.Transparent.TXOutput.ToTXReadable(this);
        }

        public object ToReadable()
        {
            return new Readable.Transparent.TXOutput(this);
        }

        public object ToTXReadable()
        {
            return new Readable.Transparent.TXOutput(this, true);
        }

        public static TTXOutput FromReadable(string json)
        {
            return Discreet.Readable.Transparent.TXOutput.FromReadable(json);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            TransactionSrc = reader.ReadSHA256();
            Address = new(reader.ReadByteArray(25));
            Amount = reader.ReadUInt64();
        }

        public void TXUnmarshal(ref MemoryReader reader)
        {
            Address = new(reader.ReadByteArray(25));
            Amount = reader.ReadUInt64();
        }

        public uint TXUnmarshal(byte[] bytes, uint offset)
        {
            var reader = new MemoryReader(bytes.AsMemory().Slice((int)offset, TXSize));
            TXUnmarshal(ref reader);
            return offset + (uint)TXSize;
        }

        public static uint GetSize()
        {
            return 33 + 32;
        }

        public int Size => (int)GetSize();
        public int TXSize => 33;
    }
}
