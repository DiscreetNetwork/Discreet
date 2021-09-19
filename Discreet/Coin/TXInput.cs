using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;

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
            return SHA256.HashData(Marshal());
        }

        public byte[] Marshal()
        {
            byte[] rv = new byte[Size()];

            for (int i = 0; i < 64; i++)
            {
                byte[] offset = BitConverter.GetBytes(Offsets[i]);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(offset);
                }

                Array.Copy(offset, 0, rv, i*((Config.OutputVersion > 1) ? 8 : 4), (Config.OutputVersion > 1) ? 8 : 4);
            }

            Array.Copy(KeyImage.bytes, 0, rv, ((Config.OutputVersion > 1) ? 8 : 4)*64, 32);

            return rv;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            for (int i = 0; i < 64; i++)
            {
                byte[] _offset = BitConverter.GetBytes(Offsets[i]);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(_offset);
                }

                Array.Copy(_offset, 0, bytes, offset + i * ((Config.OutputVersion > 1) ? 8 : 4), (Config.OutputVersion > 1) ? 8 : 4);
            }

            Array.Copy(KeyImage.bytes, 0, bytes, offset + ((Config.OutputVersion > 1) ? 8 : 4) * 64, 32);
        }

        public string Readable()
        {
            string rv = "{\"Offsets\":[";
            for (int i = 0; i < 64; i++)
            {
                rv += Offsets[i].ToString();
                if (i < 63)
                {
                    rv += ",";
                }
            }
            rv += $"],\"KeyImage\":\"{KeyImage.ToHex()}\"}}";
            return rv;
        }

        public void Unmarshal(byte[] bytes)
        {
            Offsets = new uint[64];
            for (int i = 0; i < 64; i++)
            {
                byte[] _offset = new byte[((Config.OutputVersion > 1) ? 8 : 4)];
                Array.Copy(bytes, ((Config.OutputVersion > 1) ? 8 : 4) * i, _offset, 0, (Config.OutputVersion > 1) ? 8 : 4);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(_offset);
                }

                Offsets[i] = BitConverter.ToUInt32(_offset);
            }

            KeyImage = new Key(new byte[32]);

            Array.Copy(bytes, ((Config.OutputVersion > 1) ? 8 : 4) * 64, KeyImage.bytes, 0, 32);
        }

        public void Unmarshal(byte[] bytes, uint offset)
        {
            Offsets = new uint[64];
            for (int i = 0; i < 64; i++)
            {
                byte[] _offset = new byte[((Config.OutputVersion > 1) ? 8 : 4)];
                Array.Copy(bytes, offset + ((Config.OutputVersion > 1) ? 8 : 4) * i, _offset, 0, (Config.OutputVersion > 1) ? 8 : 4);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(_offset);
                }

                Offsets[i] = BitConverter.ToUInt32(_offset);
            }

            KeyImage = new Key(new byte[32]);

            Array.Copy(bytes, offset + ((Config.OutputVersion > 1) ? 8 : 4) * 64, KeyImage.bytes, 0, 32);
        }

        public static uint Size()
        {
            return (uint)((Config.OutputVersion > 1) ? 8 : 4) * 64 + 32;
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
    }
}
