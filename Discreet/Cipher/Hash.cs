using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    public interface HashCtx 
    {
        public int Init();
        public int Update(byte[] data);
        public int Final(byte[] dataout);
    }
    public interface Hash
    {
        /* safely returns a copy of the hash data */
        public byte[] GetBytes();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SHA256Ctx: HashCtx
    {
        [MarshalAs(UnmanagedType.U4)]
        private uint count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        private byte[] buf;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
        private uint[] s;

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha256_init", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha256_init(SHA256Ctx state);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha256_update", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha256_update(SHA256Ctx state, [In, Out] [MarshalAs(UnmanagedType.LPArray)] byte[] _in, ulong inlen);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha256_update", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha256_final(SHA256Ctx state, [In, Out] [MarshalAs(UnmanagedType.LPArray, SizeConst = 32)] byte[] _out);

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

    public struct SHA256 : Hash
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

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha256", CallingConvention = CallingConvention.Cdecl)]
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
                Console.WriteLine("ERR: Discreet.Cipher.SHA256 cannot have data be anything but 32 bytes");
                bytes = new byte[32];
            }
            else
            {
                bytes = data;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SHA512Ctx : HashCtx
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U8)]
        private ulong[] count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        private byte[] buf;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U8)]
        private uint[] s;

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha512_init", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha512_init(SHA512Ctx state);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha512_update", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha512_update(SHA512Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] _in, ulong inlen);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha512_update", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha512_final(SHA512Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 64)] byte[] _out);

        public int Init()
        {
            return sha512_init(this);
        }

        public int Update(byte[] data)
        {
            return sha512_update(this, data, (ulong)data.Length);
        }

        public int Final(byte[] dataout)
        {
            return sha512_final(this, dataout);
        }
    }

    public struct SHA512 : Hash
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
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

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha512", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha512([In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 64)] byte[] dataout, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] datain, ulong len);

        public static SHA512 HashData(byte[] data)
        {
            byte[] _bytes = new byte[64];
            sha512(_bytes, data, (ulong)data.Length);
            return new SHA512(_bytes, false);
        }

        public SHA512(byte[] data, bool hash)
        {
            if (hash)
            {
                bytes = new byte[64];
                sha512(bytes, data, (ulong)data.Length);
            }
            else if (data.Length != 64)
            {
                Console.WriteLine("ERR: Discreet.Cipher.SHA512 cannot have data be anything but 64 bytes");
                bytes = new byte[64];
            }
            else
            {
                bytes = data;
            }
        }
    }
}
