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
        public ulong DecodedAmount;
        public ulong Amount;

        public bool Encrypted;

        /* stealth transactions */
        public uint Index;
        public Cipher.Key UXKey;
        public Cipher.Key Commitment;

        /* default constructor always sets UTXOType to STEALTH */
        public UTXO(uint index, Coin.TXOutput output)
        {
            Type = UTXOType.STEALTH;
            TXIndex = index;
            TransactionSrc = output.TransactionSrc;
            UXKey = output.UXKey;
            Commitment = output.Commitment;
            Amount = output.Amount;
            DecodedAmount = 0;

            Encrypted = true;
        }
    }
}
