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

        private static int keccak_init(KeccakCtx state) => Native.Native.Instance.keccak_init(state);
        private static int keccak_update(KeccakCtx state, byte[] _in, ulong inlen) => Native.Native.Instance.keccak_update(state, _in, inlen);
        private static int keccak_final(KeccakCtx state, byte[] _out) => Native.Native.Instance.keccak_final(state, _out);

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

        private static void keccak(byte[] datain, uint inlen, byte[] dataout, uint dlen) => Native.Native.Instance.keccak(datain, inlen, dataout, dlen);

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
