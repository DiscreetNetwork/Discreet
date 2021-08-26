using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Coin
{
    /**
     * WIP
     * 
     * Transaction outputs come in both transparent and private varieties.
     * 
     * 
     * 
     * 
     * 
     */

    [StructLayout(LayoutKind.Sequential)]
    public class TxOutputHeader
    {
        [MarshalAs(UnmanagedType.U8)]
        public ulong Time;
        [MarshalAs(UnmanagedType.U8)]
        public ulong BlockNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class TxOutput
    {
        [MarshalAs(UnmanagedType.Struct)]
        Discreet.Cipher.SHA256 TransactionSrc;
        [MarshalAs(UnmanagedType.Struct)]
        Discreet.Cipher.Key Value;
    }

}
