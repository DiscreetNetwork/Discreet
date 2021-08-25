using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Signature
    {
        [MarshalAs(UnmanagedType.Struct)]
        public Key s;
        [MarshalAs(UnmanagedType.Struct)]
        public Key e;

        public Signature(Key x, Key p, string m)
        {
            this = KeyOps.Sign(ref x, ref p, m);
        }

        public Signature(Key x, Key p, byte[] m)
        {
            this = KeyOps.Sign(ref x, ref p, m);
        }

        public Signature(Key x, Key p, Hash m)
        {
            this = KeyOps.Sign(ref x, ref p, m);
        }

        public Signature(Key x, Key p, SHA256 m)
        {
            this = KeyOps.Sign(ref x, ref p, m);
        }

        public Signature(Key x, Key p, Key m)
        {
            this = KeyOps.Sign(ref x, ref p, m);
        }

        public bool Verify(Key p, byte[] m)
        {
            Key mk = KeyOps.SHA256ToKey(SHA256.HashData(m));

            return KeyOps.SchnorrVerify(ref s, ref e, ref p, ref mk);
        }

        public bool Verify(Key p, SHA256 m)
        {
            Key mk = KeyOps.SHA256ToKey(m);
            return KeyOps.SchnorrVerify(ref s, ref e, ref p, ref mk);
        }

        public bool Verify(Key p, string m)
        {
            byte[] bytes = UTF8Encoding.UTF8.GetBytes(m);
            Key mk = KeyOps.SHA256ToKey(SHA256.HashData(bytes));

            return KeyOps.SchnorrVerify(ref s, ref e, ref p, ref mk);
        }

        public bool Verify(Key p, Hash m)
        {
            byte[] bytes = m.GetBytes();
            Key mk = KeyOps.SHA256ToKey(SHA256.HashData(bytes));

            return KeyOps.SchnorrVerify(ref s, ref e, ref p, ref mk);
        }

        public bool Verify(Key p, Key m)
        {
            return KeyOps.SchnorrVerify(ref s, ref e, ref p, ref m);
        }
    }
}
