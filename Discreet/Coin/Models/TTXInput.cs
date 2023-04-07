using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using Discreet.Cipher.Extensions;
using System.Diagnostics.CodeAnalysis;
using Discreet.Common.Serialize;

namespace Discreet.Coin.Models
{
    public class TTXInput : IComparable<TTXInput>, IHashable
    {
        public Cipher.SHA256 TxSrc { get; set; }
        public byte Offset { get; set; }

        public TTXInput() { }

        public TTXInput(byte[] data)
        {
            if (data == null || data.Length != 33) throw new ArgumentException("data");
            TxSrc = new Cipher.SHA256(data[0..32], false);
            Offset = data[32];
        }

        public Cipher.SHA256 Hash(TTXOutput txo)
        {
            byte[] hshdat = new byte[66];
            Array.Copy(TxSrc.Bytes, hshdat, 32);
            hshdat[32] = Offset;
            Array.Copy(txo.TXMarshal(), 0, hshdat, 33, 33);

            return Cipher.SHA256.HashData(hshdat);
        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteSHA256(TxSrc);
            writer.Write(Offset);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            TxSrc = reader.ReadSHA256();
            Offset = reader.ReadUInt8();
        }

        public static uint GetSize() => 33;
        public int Size => (int)GetSize();

        public string ToReadable()
        {
            return Common.Printable.Hexify(this.Serialize());
        }

        public int CompareTo(TTXInput other)
        {
            return this.Serialize().Compare(other.Serialize());
        }

        public override bool Equals(object obj)
        {
            if (obj is TTXInput other)
            {
                return CompareTo(other) == 0;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (int)((uint)TxSrc.Bytes[0] << 24 | (uint)TxSrc.Bytes[1] << 16 | (uint)TxSrc.Bytes[2] << 8 | Offset);
        }

        public static bool operator ==(TTXInput a, TTXInput b) => a.Equals(b);
        public static bool operator !=(TTXInput a, TTXInput b) => !a.Equals(b);

        public static bool operator >(TTXInput a, TTXInput b) => a.CompareTo(b) > 0;
        public static bool operator <(TTXInput a, TTXInput b) => a.CompareTo(b) < 0;
        public static bool operator >=(TTXInput a, TTXInput b) => a.CompareTo(b) >= 0;
        public static bool operator <=(TTXInput a, TTXInput b) => a.CompareTo(b) <= 0;
    }
}
