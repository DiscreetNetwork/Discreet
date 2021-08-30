using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;

namespace Discreet.Coin
{
    [StructLayout(LayoutKind.Sequential)]
    public class Triptych : ICoin
    {
        [MarshalAs(UnmanagedType.Struct)]
        public Key J;

        [MarshalAs(UnmanagedType.Struct)]
        public Key K;

        [MarshalAs(UnmanagedType.Struct)]
        public Key A, B, C, D;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.Struct)]
        public Key[] X, Y, f;

        [MarshalAs(UnmanagedType.Struct)]
        public Key zA, zC, z;

        public SHA256 Hash()
        {
            return SHA256.HashData(Marshal());
        }

        public byte[] Marshal()
        {
            byte[] bytes = new byte[Size()];

            Array.Copy(J.bytes, 0, bytes, 0, 32);
            Array.Copy(K.bytes, 0, bytes, 32, 32);
            Array.Copy(A.bytes, 0, bytes, 32 * 2, 32);
            Array.Copy(B.bytes, 0, bytes, 32 * 3, 32);
            Array.Copy(C.bytes, 0, bytes, 32 * 4, 32);
            Array.Copy(D.bytes, 0, bytes, 32 * 5, 32);

            X = new Key[6];
            Y = new Key[6];
            f = new Key[6];

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(X[i].bytes, 0, bytes, 32 * 6 + 32 * i, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(Y[i].bytes, 0, bytes, 32 * 12 + 32 * i, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(Y[i].bytes, 0, bytes, 32 * 18 + 32 * i, 32);
            }

            Array.Copy(zA.bytes, 0, bytes, 24 * 32, 32);
            Array.Copy(zC.bytes, 0, bytes, 25 * 32, 32);
            Array.Copy(z.bytes, 0, bytes, 26 * 32, 32);

            return bytes;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            byte[] _bytes = Marshal();
            Array.Copy(_bytes, 0, bytes, offset, _bytes.Length);
        }

        public string Readable()
        {
            string rv = "{";
            rv += $"\"J\":\"{J.ToHex()}\",";
            rv += $"\"K\":\"{K.ToHex()}\",";
            rv += $"\"A\":\"{A.ToHex()}\",";
            rv += $"\"B\":\"{B.ToHex()}\",";
            rv += $"\"C\":\"{C.ToHex()}\",";
            rv += $"\"D\":\"{D.ToHex()}\",";
            rv += "\"X\":[";

            for (int i = 0; i < 6; i++)
            {
                rv += $"\"{X[i].ToHex()}\"";
                if (i < 5)
                {
                    rv += ",";
                }
            }

            rv += "],\"Y\":[";

            for (int i = 0; i < 6; i++)
            {
                rv += $"\"{Y[i].ToHex()}\"";
                if (i < 5)
                {
                    rv += ",";
                }
            }

            rv += "],\"f\":[";

            for (int i = 0; i < 6; i++)
            {
                rv += $"\"{f[i].ToHex()}\"";
                if (i < 5)
                {
                    rv += ",";
                }
            }

            rv += $"],\"zA\":\"{zA.ToHex()}\",";
            rv += $"\"zC\":\"{zC.ToHex()}\",";
            rv += $"\"z\":\"{z.ToHex()}\"}}";

            return rv;
        }

        public void Unmarshal(byte[] bytes)
        {
            Array.Copy(bytes, 0, J.bytes, 0, 32);
            Array.Copy(bytes, 32, K.bytes, 0, 32);
            Array.Copy(bytes, 32 * 2, A.bytes, 0, 32);
            Array.Copy(bytes, 32 * 3, B.bytes, 0, 32);
            Array.Copy(bytes, 32 * 4, C.bytes, 0, 32);
            Array.Copy(bytes, 32 * 5, D.bytes, 0, 32);

            X = new Key[6];
            Y = new Key[6];
            f = new Key[6];

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(bytes, 32 * 6 + 32 * i, X[i].bytes, 0, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(bytes, 32 * 12 + 32 * i, Y[i].bytes, 0, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(bytes, 32 * 18 + 32 * i, Y[i].bytes, 0, 32);
            }

            Array.Copy(bytes, 24 * 32, zA.bytes, 0, 32);
            Array.Copy(bytes, 25 * 32, zC.bytes, 0, 32);
            Array.Copy(bytes, 26 * 32, z.bytes, 0, 32);
        }

        public void Unmarshal(byte[] bytes, uint offset)
        {
            Array.Copy(bytes, offset, J.bytes, 0, 32);
            Array.Copy(bytes, offset + 32, K.bytes, 0, 32);
            Array.Copy(bytes, offset + 32 * 2, A.bytes, 0, 32);
            Array.Copy(bytes, offset + 32 * 3, B.bytes, 0, 32);
            Array.Copy(bytes, offset + 32 * 4, C.bytes, 0, 32);
            Array.Copy(bytes, offset + 32 * 5, D.bytes, 0, 32);

            X = new Key[6];
            Y = new Key[6];
            f = new Key[6];

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(bytes, offset + 32 * 6 + 32 * i, X[i].bytes, 0, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(bytes, offset + 32 * 12 + 32 * i, Y[i].bytes, 0, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(bytes, offset + 32 * 18 + 32 * i, Y[i].bytes, 0, 32);
            }

            Array.Copy(bytes, offset + 24 * 32, zA.bytes, 0, 32);
            Array.Copy(bytes, offset + 25 * 32, zC.bytes, 0, 32);
            Array.Copy(bytes, offset + 26 * 32, z.bytes, 0, 32);
        }

        public static uint Size()
        {
            return 18*64 + 9*64;
        }
    }
}
