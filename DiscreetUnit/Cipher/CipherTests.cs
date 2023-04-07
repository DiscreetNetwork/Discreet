﻿using Discreet.Cipher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscreetUnit.Cipher
{
    public class CipherTests
    {
        [Fact]
        public void TestScalarmultBase()
        {
            Key testsk = new Key(new byte[] { 0x64, 0x11, 0xcd, 0x11, 0x99, 0x97, 0x7b, 0x89, 0xe9, 0x19, 0x3d, 0xcf, 0x4e, 0xd8, 0x41, 0x7d, 0x3c, 0x76, 0x60, 0x44, 0x48, 0x10, 0xf0, 0xc8, 0xc4, 0xa3, 0x14, 0x80, 0x0e, 0x06, 0x99, 0x07 });
            Key testpk = new Key(new byte[] { 0xb5, 0x30, 0x43, 0x1b, 0x3b, 0x66, 0xc4, 0x48, 0x6b, 0x39, 0x13, 0x9b, 0x9d, 0xfd, 0x7f, 0x84, 0xdc, 0x44, 0x88, 0x16, 0x79, 0xb2, 0xc2, 0x18, 0x33, 0xee, 0xfe, 0x1f, 0x30, 0xf6, 0xb3, 0x1c });

            Key pk = KeyOps.ScalarmultBase(ref testsk);

            Assert.True(testpk.Equals(pk));

            Key pknew = KeyOps.GeneratePubkey();

            Assert.False(testpk.Equals(pknew));
        }

    }
}
