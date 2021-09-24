using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Wallet
{
    public class UnsignedTX
    {
        public Cipher.Key TransactionKey;
        public ulong Fee;

        /* inputs to be signed */
        public uint[] InputIDs;
        public Cipher.Key[] InputKeys;
        public Cipher.Key[] InputCommitments;
        public Cipher.Key[] InputTransactionKeys;

        /* Outputs */
        public Cipher.Key[] OutputCommitments;
        public Cipher.Key[] OutputKeys;

        /* Psuedo outs */
        public Cipher.Key[] PseudoOuts;

        /* range proof */
        public Coin.Bulletproof RangeProof;

        /* additional information */
        
    }
}
