using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    [StructLayout(LayoutKind.Sequential)]
    public struct KeccakCtx : HashCtx
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25, ArraySubType = UnmanagedType.U8)]
        private uint[] st;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17, ArraySubType = UnmanagedType.U8)]
        private byte[] buf;
        [MarshalAs(UnmanagedType.U4)]
        private uint rest;

        [DllImport(@"DiscreetCore.dll", EntryPoint = "keccak_init", CallingConvention = CallingConvention.StdCall)]
        private static extern int keccak_init(KeccakCtx state);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "keccak_update", CallingConvention = CallingConvention.StdCall)]
        private static extern int keccak_update(KeccakCtx state, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] _in, ulong inlen);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "keccak_final", CallingConvention = CallingConvention.StdCall)]
        private static extern int keccak_final(KeccakCtx state, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 32)] byte[] _out);

        public int Init()
        {
            return keccak_init(this);
        }

        public int Update(byte[] data)
        {
            return keccak_update(this, data, (ulong)data.Length);
        }

        public int Final(byte[] dataout)
        {
            return keccak_final(this, dataout);
        }
    }

    public struct Keccak : Hash
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

        [DllImport(@"DiscreetCore.dll", EntryPoint = "keccak", CallingConvention = CallingConvention.StdCall)]
        private static extern void keccak([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] datain, uint inlen, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] dataout, uint dlen);

        public static Keccak HashData(byte[] data)
        {
            byte[] _bytes = new byte[32];
            keccak(data, (uint)data.Length, _bytes, 32);
            return new Keccak(_bytes, false);
        }

        public Keccak(byte[] data, bool hash)
        {
            if (hash)
            {
                bytes = new byte[32];
                keccak(data, (uint)data.Length, bytes, 32);
            }
            else if (data.Length != 32)
            {
                throw new Exception("ERR: Discreet.Cipher.Keccak cannot have data be anything but 32 bytes");
            }
            else
            {
                bytes = data;
            }
        }

        public string ToHex()
        {
            return BitConverter.ToString(GetBytes()).Replace("-", string.Empty).ToLower();
        }

        public string ToHexShort()
        {
            return ToHex().Substring(0, 8);
        }
    }
}
