using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Coin
{
    public interface ICoin
    {
        public Discreet.Cipher.SHA256 Hash();
        public string Readable();
        public byte[] Marshall();
        public void Unmarshall(byte[] bytes);
        public uint Size();
    }
}
