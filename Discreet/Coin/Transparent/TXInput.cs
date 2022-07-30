using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using Discreet.Cipher.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace Discreet.Coin.Transparent
{
    [StructLayout(LayoutKind.Sequential)]
    public class TXInput: IComparable<TXInput>
    {
        [MarshalAs(UnmanagedType.Struct)]
        public Cipher.SHA256 TxSrc;

        [MarshalAs(UnmanagedType.U1)]
        public byte Offset;

        public TXInput() { }

        public TXInput(Cipher.SHA256 txid, byte offset)
        {
            TxSrc = txid;
            Offset = offset;
        }

        public Cipher.SHA256 Hash(TXOutput txo)
        {
            byte[] hshdat = new byte[66];
            Array.Copy(TxSrc.Bytes, hshdat, 32);
            hshdat[32] = Offset;
            Array.Copy(txo.TXMarshal(), 0, hshdat, 33, 33);

            return Cipher.SHA256.HashData(hshdat);
        }

        public byte[] Serialize()
        {
            byte[] rv = new byte[33];
            Array.Copy(TxSrc.Bytes, rv, 32);
            rv[32] = Offset;

            return rv;
        }

        public void Serialize(byte[] data, uint offset)
        {
            Array.Copy(TxSrc.Bytes, 0, data, offset, 32);
            data[offset + 32] = Offset;
        }

        public uint Deserialize(byte[] data, uint offset)
        {
            TxSrc = new Cipher.SHA256(data, offset);
            Offset = data[offset + 32];

            return offset + 33;
        }

        public void Deserialize(byte[] data)
        {
            Deserialize(data, 0);
        }

        public void Serialize(Stream s)
        {
            s.Write(TxSrc.Bytes);
            s.WriteByte(Offset);
        }

        public void Deserialize(Stream s)
        {
            TxSrc = new Cipher.SHA256(s);
            Offset = (byte)s.ReadByte();
        }

        public static uint Size() => 33;

        public override string ToString()
        {
            return $"({Offset}){TxSrc.ToHexShort()}";
        }

        public int CompareTo(TXInput other)
        {
            return Serialize().Compare(other.Serialize());
        }

        public override bool Equals(object obj)
        {
            if (obj is TXInput other)
            {
                return CompareTo(other) == 0;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (int)(((uint)TxSrc.Bytes[0] << 24) | ((uint)TxSrc.Bytes[1] << 16) | ((uint)TxSrc.Bytes[2] << 8) | Offset);
        }

        public static bool operator ==(TXInput a, TXInput b) => a.Equals(b);
        public static bool operator !=(TXInput a, TXInput b) => !a.Equals(b);

        public static bool operator >(TXInput a, TXInput b) => a.CompareTo(b) > 0;
        public static bool operator <(TXInput a, TXInput b) => a.CompareTo(b) < 0;
        public static bool operator >=(TXInput a, TXInput b) => a.CompareTo(b) >= 0;
        public static bool operator <=(TXInput a, TXInput b) => a.CompareTo(b) <= 0;
    }

    public class TXInputEqualityComparer : IEqualityComparer<TXInput>
    {
        public bool Equals(TXInput x, TXInput y) => x.Equals(y);

        public int GetHashCode([DisallowNull] TXInput obj) => Cipher.SHA256.HashData(obj.Serialize()).GetHashCode();
    }
}
