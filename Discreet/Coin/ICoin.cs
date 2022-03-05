using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Discreet.Coin
{
    public interface ICoin
    {
        public Discreet.Cipher.SHA256 Hash();
        public string Readable();
        public object ToReadable();
        public byte[] Serialize();
        public void Serialize(byte[] bytes, uint offset);
        public void Deserialize(byte[] bytes);
        public uint Deserialize(byte[] bytes, uint offset);
        public void Serialize(Stream s);
        public void Deserialize(Stream s);
        public static uint Size() { return 0; }

        public VerifyException Verify();
    }
}
