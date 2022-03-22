using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    public static class KeyDerivation
    {
        public static void PBKDF2(byte[] output, byte[] password, uint password_len, byte[] salt, uint salt_len, uint num_iterations, uint key_len)
            => Native.Native.Instance.pbkdf2(output, password, password_len, salt, salt_len, num_iterations, key_len);
    }
}
