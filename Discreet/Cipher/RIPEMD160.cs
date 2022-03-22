using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RIPEMD160Ctx
    {
        [MarshalAs(UnmanagedType.U8)]
        private ulong length;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        private byte[] buf;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.U4)]
        private uint[] h;

        [MarshalAs(UnmanagedType.U1)]
        private byte bufpos;

        private static void ripemd160_init(RIPEMD160Ctx state) => Native.Native.Instance.ripemd160_init(state);
        private static void ripemd160_update(RIPEMD160Ctx state, byte[] _in, ulong inlen) => Native.Native.Instance.ripemd160_update(state, _in, inlen);
        private static void ripemd160_final(RIPEMD160Ctx state, byte[] _out) => Native.Native.Instance.ripemd160_final(state, _out);

        public void Init()
        {
            ripemd160_init(this);
        }

        public void Update(byte[] data)
        {
            ripemd160_update(this, data, (ulong)data.Length);
        }

        public void Final(byte[] dataout)
        {
            ripemd160_final(this, dataout);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RIPEMD160: Hash
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
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

        private static void ripemd160(byte[] datain, ulong inlen, byte[] dataout) => Native.Native.Instance.ripemd160(datain, inlen, dataout);

        public static RIPEMD160 HashData(byte[] data)
        {
            byte[] _bytes = new byte[20];
            ripemd160(data, (ulong)data.Length, _bytes);
            return new RIPEMD160(_bytes, false);
        }

        public RIPEMD160(byte[] data, bool hash)
        {
            if (hash)
            {
                bytes = new byte[20];
                ripemd160(data, (ulong)data.Length, bytes);
            }
            else if (data.Length != 20)
            {
                throw new Exception("ERR: Discreet.Cipher.RIPEMD160 cannot have data be anything but 20 bytes");
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
