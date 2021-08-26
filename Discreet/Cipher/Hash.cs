using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    public interface HashCtx 
    {
        public int Init();
        public int Update(byte[] data);
        public int Final(byte[] dataout);
    }

    public interface Hash
    {
        /* safely returns a copy of the hash data */
        public byte[] GetBytes();

        public string ToHex();

        public string ToHexShort();
    }
}
