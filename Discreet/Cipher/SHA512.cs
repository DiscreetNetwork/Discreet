﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SHA512Ctx : HashCtx
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U8)]
        private ulong[] count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        private byte[] buf;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U8)]
        private uint[] s;

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha512_init", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern int sha512_init(SHA512Ctx state);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha512_update", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern int sha512_update(SHA512Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] _in, ulong inlen);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha512_update", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I4)]
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

        [DllImport(@"DiscreetCore.dll", EntryPoint = "sha512", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I4)]
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
                throw new Exception("Discreet.Cipher.SHA512 cannot have data be anything but 64 bytes");
            
            }
            else
            {
                bytes = data;
            }
        }
    }
}
