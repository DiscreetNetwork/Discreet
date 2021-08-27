using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Coin
{
    public interface ICoin
    {
        public Discreet.Cipher.SHA256 Hash();
        public string Readable();
        public byte[] Marshal();
        public void Marshal(byte[] bytes, uint offset);
        public void Unmarshal(byte[] bytes);
        public void Unmarshal(byte[] bytes, uint offset);
        public static uint Size() { return 0; }
    }
}
