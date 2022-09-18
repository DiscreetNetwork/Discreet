using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace Discreet.Cipher
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SHA256Ctx : HashCtx 
    {
        [MarshalAs(UnmanagedType.U4)]
        private uint count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        private byte[] buf;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
        private uint[] s;

        private static int sha256_init(SHA256Ctx state) => Native.Native.Instance.sha256_init(state);
        private static int sha256_update(SHA256Ctx state, byte[] _in, ulong inlen) => Native.Native.Instance.sha256_update(state, _in, inlen);
        private static int sha256_final(SHA256Ctx state, byte[] _out) => Native.Native.Instance.sha256_final(state, _out);

        public int Init()
        {
            return sha256_init(this);
        }

        public int Update(byte[] data)
        {
            return sha256_update(this, data, (ulong)data.Length);
        }

        public int Final(byte[] dataout)
        {
            return sha256_final(this, dataout);
        }
    }

    public struct SHA256 : Hash, IComparable, IComparable<SHA256>
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        private byte[] bytes;

        /* a property which returns a pointer to the hash data. Do not use unless necessary. */
        public byte[] Bytes
        {
            get
            {
                return bytes;
            }
        }

        public byte[] GetBytes() { return bytes; }

        private static int sha256(byte[] dataout, byte[] datain, ulong len) => Native.Native.Instance.sha256(dataout, datain, len);

        public static SHA256 HashData(byte[] data)
        {
            byte[] _bytes = new byte[32];
            sha256(_bytes, data, (ulong)data.Length);
            return new SHA256(_bytes, false);
        }

        public static SHA256 HashData(params byte[][] data)
        {
            int sz = 0;
            for (int i = 0; i < data.Length; i++)
            {
                sz += data[i].Length;
            }

            byte[] d2h = new byte[sz];
            sz = 0;
            for (int i = 0; i < data.Length; i++)
            {
                Array.Copy(data[i], 0, d2h, sz, data[i].Length);
                sz += data[i].Length;
            }

            return HashData(d2h);
        }

        public SHA256(byte[] data, bool hash)
        {
            if (hash)
            {
                bytes = new byte[32];
                sha256(bytes, data, (ulong)data.Length);
            }
            else if (data.Length != 32)
            {
                throw new Exception("ERR: Discreet.Cipher.SHA256 cannot have data be anything but 32 bytes");
            }
            else
            {
                bytes = data;
            }
        }

        public SHA256(Stream s)
        {
            bytes = new byte[32];
            s.Read(bytes);
        }

        public SHA256(byte[] data, uint offset)
        {
            bytes = new byte[32];
            Array.Copy(data, offset, bytes, 0, 32);
        }

        public SHA256(ulong num)
        {
            bytes = new byte[32];
            Coin.Serialization.CopyData(bytes, 24, num);
        }

        public SHA256(long num)
        {
            bytes = new byte[32];
            Coin.Serialization.CopyData(bytes, 24, num);
        }

        public ulong ToUInt64()
        {
            return Coin.Serialization.GetUInt64(bytes, 24);
        }

        public long ToInt64()
        {
            return Coin.Serialization.GetInt64(bytes, 24);
        }

        public bool IsLong()
        {
            for (int i = 0; i < 24; i++)
            {
                if (bytes[i] != 0) return false;
            }

            return true;
        }

        public string ToHex()
        {
            return BitConverter.ToString(GetBytes()).Replace("-", string.Empty).ToLower();
        }

        public string ToHexShort()
        {
            return ToHex().Substring(0, 8) + "...";
        }

        /* a > b => 1, a == b => 0, a < b => -1 */
        public static int Compare(SHA256 a, SHA256 b)
        {
            if (a.bytes == null && b.bytes == null) return 0;
            if (a.bytes == null) return -1;
            if (b.bytes == null) return 1;

            int i;

            for (i = 0; i < 32; i++)
            {
                if (a.bytes[i] != b.bytes[i])
                {
                    break;
                }
            }

            if (i == 32)
            {
                return 0;
            }
            else if (a.bytes[i] > b.bytes[i])
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

#nullable enable
        public int CompareTo(object? b)
        {
            if (b == null || b == default) throw new Exception("Discreet.Cipher.SHA256.CompareTo: cannot compare to null or defaulted object");

            return Compare(this, (SHA256)b);
        }
#nullable disable

        public override bool Equals(object b)
        {
            if (b == default && bytes == null) return true;
            if (b == default) return false;
            if (bytes == null) return false;
            if (b is SHA256 bs) return Compare(this, bs) == 0;

            return false;
        }

        public static bool Equals(SHA256 a, SHA256 b)
        {
            return a.Equals(b);
        }

        public Key ToKey()
        {
            return new Key(bytes);
        }

        public static SHA256 FromHex(string hex)
        {
            return new SHA256(Common.Printable.Byteify(hex), false);
        }

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(bytes[0..4]);
        }

        public int CompareTo(SHA256 other) => Compare(this, other);

        public static bool operator ==(SHA256 a, SHA256 b) => SHA256.Equals(a, b);

        public static bool operator !=(SHA256 a, SHA256 b) => !SHA256.Equals(a, b);

        public static bool operator >(SHA256 a, SHA256 b) => SHA256.Compare(a, b) > 0;

        public static bool operator <(SHA256 a, SHA256 b) => SHA256.Compare(a, b) < 0;

        public static bool operator >=(SHA256 a, SHA256 b) => SHA256.Compare(a, b) >= 0;

        public static bool operator <=(SHA256 a, SHA256 b) => SHA256.Compare(a, b) <= 0;

    }
}
