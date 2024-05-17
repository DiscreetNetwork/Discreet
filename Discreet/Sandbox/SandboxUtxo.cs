using Discreet.Cipher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Sandbox
{
    public class SandboxUtxo
    {
        public bool IsCoinbase { get; set; }
        public byte Type { get; set; }

        public SHA256 TxSrc { get; set; }
        public ulong Amount { get; set; }
        public uint OutputIndex { get; set; }

        public Key UXKey;
        public Key UXSecKey;
        public Key Commitment;

        public Key TxKey;
        public int DecodeIndex;
        public ulong DecodedAmount;
        public Key LinkingTag;

        internal SandboxWallet Wallet;
    }
}
