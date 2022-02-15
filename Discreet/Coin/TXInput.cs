using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.IO;

namespace Discreet.Coin
{
    [StructLayout(LayoutKind.Sequential)]
    public class TXInput : ICoin
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U4)]
        public uint[] Offsets;

        [MarshalAs(UnmanagedType.Struct)]
        public Key KeyImage;

        public SHA256 Hash()
        {
            return SHA256.HashData(Serialize());
        }

        public byte[] Serialize()
        {
            byte[] rv = new byte[Size()];

            for (int i = 0; i < 64; i++)
            {
                Serialization.CopyData(rv, (uint)(i * 4), Offsets[i]);
            }

            Array.Copy(KeyImage.bytes, 0, rv, 4*64, 32);

            return rv;
        }

        public void Serialize(byte[] bytes, uint offset)
        {
            byte[] rv = Serialize();
            Array.Copy(rv, 0, bytes, offset, rv.Length);
        }

        public string Readable()
        {
            return Discreet.Readable.TXInput.ToReadable(this);
        }

        public static TXInput FromReadable(string json)
        {
            return Discreet.Readable.TXInput.FromReadable(json);
        }

        public void Deserialize(byte[] bytes)
        {
            Deserialize(bytes, 0);
        }

        public uint Deserialize(byte[] bytes, uint offset)
        {
            Offsets = new uint[64];
            for (int i = 0; i < 64; i++)
            {
                Offsets[i] = Serialization.GetUInt32(bytes, offset + (uint)(i * 4));
            }

            KeyImage = new Key(bytes, offset + 4 * 64);

            return offset + Size();
        }

        public void Serialize(Stream s)
        {
            for (int i = 0; i < 64; i++)
            {
                Serialization.CopyData(s, Offsets[i]);
            }

            s.Write(KeyImage.bytes);
        }

        public void Deserialize(Stream s)
        {
            Offsets = new uint[64];
            for (int i = 0; i < 64; i++)
            {
                Offsets[i] = Serialization.GetUInt32(s);
            }

            KeyImage = new Key(s);
        }

        public static uint Size()
        {
            return 4 * 64 + 32;
        }

        public static TXInput GenerateMock()
        {
            TXInput input = new TXInput();

            input.KeyImage = Cipher.KeyOps.GeneratePubkey();
            input.Offsets = new uint[64];
            System.Random rng = new System.Random();

            for (int i = 0; i < 64; i++)
            {
                input.Offsets[i] = (uint)rng.Next(0, Int32.MaxValue);
            }

            return input;
        }

        public VerifyException Verify()
        {
            return new VerifyException("TXInput", "UNIMPLEMENTED");
        }
    }
}
