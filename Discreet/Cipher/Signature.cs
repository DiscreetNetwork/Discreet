﻿using System;
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

        public Signature(byte[] bytes)
        {
            s = new Key(new byte[32]);
            e = new Key(new byte[32]);
            
            Array.Copy(bytes, s.bytes, 32);
            Array.Copy(bytes, 32, e.bytes, 0, 32);
        }

        public Signature(byte[] bytes, uint offset)
        {
            s = new Key(new byte[32]);
            e = new Key(new byte[32]);

            Array.Copy(bytes, offset, s.bytes, 00, 32);
            Array.Copy(bytes, offset + 32, e.bytes, 0, 32);
        }

        public Signature(bool blank)
        {
            s = new Key(new byte[32]);
            e = new Key(new byte[32]);
        }

        public Signature(Key _s, Key _e)
        {
            s = _s;
            e = _e;
        }

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

        public byte[] ToBytes()
        {
            byte[] bytes = new byte[64];
            Array.Copy(s.bytes, bytes, 32);
            Array.Copy(e.bytes, 0, bytes, 32, 32);
            return bytes;
        }

        public void ToBytes(byte[] bytes, uint offset)
        {
            Array.Copy(s.bytes, 0, bytes, offset, 32);
            Array.Copy(e.bytes, 0, bytes, offset + 32, 32);
        }
    }
}
