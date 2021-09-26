using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Wallets
{
    public enum UTXOType
    {
        //TRANSPARENT, currently unsupported in testnet. Private transactions only.
        STEALTH,
    }

    /**
     * <summary>This class stores UTXO data for wallets.</summary>
     */
    public class UTXO
    {
        /* fields are used as needed, depending on type. */
        public UTXOType Type;

        /* for all types */
        public Cipher.SHA256 TransactionSrc;
        public ulong TXIndex;
        public ulong BlockHeight;
        public Cipher.SHA256 BlockHash;
        public ulong Timestamp;

        /* stealth transactions */
        public uint Index;
        public Cipher.Key UXKey;
        public Cipher.Key Commitment;

        /* default constructor always sets UTXOType to STEALTH */
        public UTXO()
        {
            Type = UTXOType.STEALTH;
        }
    }
}
