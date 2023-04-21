using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using Discreet.Common.Serialize;

namespace Discreet.Cipher
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Signature : ISerializable
    {
        [MarshalAs(UnmanagedType.Struct)]
        public Key s;
        [MarshalAs(UnmanagedType.Struct)]
        public Key e;
        [MarshalAs(UnmanagedType.Struct)]
        public Key y;

        public Signature(byte[] bytes)
        {
            s = new Key(new byte[32]);
            e = new Key(new byte[32]);
            y = new Key(new byte[32]);
            
            Array.Copy(bytes, s.bytes, 32);
            Array.Copy(bytes, 32, e.bytes, 0, 32);
            Array.Copy(bytes, 64, y.bytes, 0, 32);
        }

        public Signature(byte[] bytes, uint offset)
        {
            s = new Key(new byte[32]);
            e = new Key(new byte[32]);
            y = new Key(new byte[32]);

            Array.Copy(bytes, offset, s.bytes, 00, 32);
            Array.Copy(bytes, offset + 32, e.bytes, 0, 32);
            Array.Copy(bytes, offset + 64, y.bytes, 0, 32);
        }

        public Signature(Stream _s)
        {
            s = new Key(_s);
            e = new Key(_s);
            y = new Key(_s);
        }

        public Signature(bool blank)
        {
            s = new Key(new byte[32]);
            e = new Key(new byte[32]);
            y = new Key(new byte[32]);
        }

        public Signature(Key _s, Key _e, Key _y, bool ignore)
        {
            s = _s;
            e = _e;
            y = _y;
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

        public bool Verify(byte[] m)
        {
            Key mk = KeyOps.SHA256ToKey(SHA256.HashData(m));

            return KeyOps.EdDSAVerify(ref s, ref e, ref y, ref mk);
        }

        public bool Verify(SHA256 m)
        {
            Key mk = KeyOps.SHA256ToKey(m);
            return KeyOps.EdDSAVerify(ref s, ref e, ref y, ref mk);
        }

        public bool Verify(string m)
        {
            byte[] bytes = UTF8Encoding.UTF8.GetBytes(m);
            Key mk = KeyOps.SHA256ToKey(SHA256.HashData(bytes));

            return KeyOps.EdDSAVerify(ref s, ref e, ref y, ref mk);
        }

        public bool Verify(Hash m)
        {
            byte[] bytes = m.GetBytes();
            Key mk = KeyOps.SHA256ToKey(SHA256.HashData(bytes));

            return KeyOps.EdDSAVerify(ref s, ref e, ref y, ref mk);
        }

        public bool Verify(Key m)
        {
            return KeyOps.EdDSAVerify(ref s, ref e, ref y, ref m);
        }

        public byte[] ToBytes()
        {
            byte[] bytes = new byte[96];
            Array.Copy(s.bytes, bytes, 32);
            Array.Copy(e.bytes, 0, bytes, 32, 32);
            Array.Copy(y.bytes, 0, bytes, 64, 32);
            return bytes;
        }

        public void ToBytes(byte[] bytes, uint offset)
        {
            Array.Copy(s.bytes, 0, bytes, offset, 32);
            Array.Copy(e.bytes, 0, bytes, offset + 32, 32);
            Array.Copy(y.bytes, 0, bytes, offset + 64, 32);
        }

        public string ToHex()
        {
            return e.ToHex() + s.ToHex() + y.ToHex();
        }

        public string ToHexShort()
        {
            return ToHex().Substring(0, 8) + "...";
        }

        public static Signature FromHex(string hex)
        {
            return new Signature(Common.Printable.Byteify(hex));
        }

        internal bool IsNull()
        {
            return s.Equals(Key.Z) && e.Equals(Key.Z);
        }

        public int Size => 96;

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteKey(s);
            writer.WriteKey(e);
            writer.WriteKey(y);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            s = reader.ReadKey();
            e = reader.ReadKey();
            y = reader.ReadKey();
        }
    }
}
