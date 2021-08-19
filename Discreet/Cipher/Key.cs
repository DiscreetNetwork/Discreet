using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Key
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] bytes;

        /* based on crypto_verify_32 (in DiscreetCore/src/crypto/verify.c) */
        public bool Equals(Key b)
        {
            int diff = 0;
            diff |= (int)bytes[0] ^ (int)b.bytes[0];
            diff |= (int)bytes[1] ^ (int)b.bytes[1];
            diff |= (int)bytes[2] ^ (int)b.bytes[2];
            diff |= (int)bytes[3] ^ (int)b.bytes[3];
            diff |= (int)bytes[4] ^ (int)b.bytes[4];
            diff |= (int)bytes[5] ^ (int)b.bytes[5];
            diff |= (int)bytes[6] ^ (int)b.bytes[6];
            diff |= (int)bytes[7] ^ (int)b.bytes[7];
            diff |= (int)bytes[8] ^ (int)b.bytes[8];
            diff |= (int)bytes[9] ^ (int)b.bytes[9];
            diff |= (int)bytes[10] ^ (int)b.bytes[10];
            diff |= (int)bytes[11] ^ (int)b.bytes[11];
            diff |= (int)bytes[12] ^ (int)b.bytes[12];
            diff |= (int)bytes[13] ^ (int)b.bytes[13];
            diff |= (int)bytes[14] ^ (int)b.bytes[14];
            diff |= (int)bytes[15] ^ (int)b.bytes[15];
            diff |= (int)bytes[16] ^ (int)b.bytes[16];
            diff |= (int)bytes[17] ^ (int)b.bytes[17];
            diff |= (int)bytes[18] ^ (int)b.bytes[18];
            diff |= (int)bytes[19] ^ (int)b.bytes[19];
            diff |= (int)bytes[20] ^ (int)b.bytes[20];
            diff |= (int)bytes[21] ^ (int)b.bytes[21];
            diff |= (int)bytes[22] ^ (int)b.bytes[22];
            diff |= (int)bytes[23] ^ (int)b.bytes[23];
            diff |= (int)bytes[24] ^ (int)b.bytes[24];
            diff |= (int)bytes[25] ^ (int)b.bytes[25];
            diff |= (int)bytes[26] ^ (int)b.bytes[26];
            diff |= (int)bytes[27] ^ (int)b.bytes[27];
            diff |= (int)bytes[28] ^ (int)b.bytes[28];
            diff |= (int)bytes[29] ^ (int)b.bytes[29];
            diff |= (int)bytes[30] ^ (int)b.bytes[30];
            diff |= (int)bytes[31] ^ (int)b.bytes[31];
            return (1 & ((diff - 1) >> 8) - 1) != 0;
        }
    }
}
