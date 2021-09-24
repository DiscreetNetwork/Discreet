using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    public static class KeyDerivation
    {
        [DllImport(@"DiscreetCore.dll", EntryPoint = "pbkdf2", CallingConvention = CallingConvention.StdCall)]
        public static extern void PBKDF2([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] output,
                                         [MarshalAs(UnmanagedType.LPArray)] byte[] password,
                                         [MarshalAs(UnmanagedType.U4)] uint password_len,
                                         [MarshalAs(UnmanagedType.LPArray)] byte[] salt,
                                         [MarshalAs(UnmanagedType.U4)] uint salt_len,
                                         [MarshalAs(UnmanagedType.U4)] uint num_iterations,
                                         [MarshalAs(UnmanagedType.U4)] uint key_len);
    }
}
