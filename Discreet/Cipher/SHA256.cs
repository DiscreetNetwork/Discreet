﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

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

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha256_init", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern int sha256_init(SHA256Ctx state);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha256_update", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern int sha256_update(SHA256Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] _in, ulong inlen);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha256_update", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern int sha256_final(SHA256Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 32)] byte[] _out);

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

    public struct SHA256 : Hash, IComparable
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

        public byte[] GetBytes() { return (byte[])bytes.Clone(); }

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha256", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern int sha256([In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 32)] byte[] dataout, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] datain, ulong len);

        public static SHA256 HashData(byte[] data)
        {
            byte[] _bytes = new byte[32];
            sha256(_bytes, data, (ulong)data.Length);
            return new SHA256(_bytes, false);
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
            return Compare(this, (SHA256)b) == 0;
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

        public static bool operator ==(SHA256 a, SHA256 b) => SHA256.Equals(a, b);

        public static bool operator !=(SHA256 a, SHA256 b) => !SHA256.Equals(a, b);

        public static bool operator >(SHA256 a, SHA256 b) => SHA256.Compare(a, b) > 0;

        public static bool operator <(SHA256 a, SHA256 b) => SHA256.Compare(a, b) < 0;

        public static bool operator >=(SHA256 a, SHA256 b) => SHA256.Compare(a, b) >= 0;

        public static bool operator <=(SHA256 a, SHA256 b) => SHA256.Compare(a, b) <= 0;

    }
}
