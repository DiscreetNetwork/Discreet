using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Discreet.Wallet
{
    public class WalletAddress
    {
        public Cipher.Key PubSpendKey;
        public Cipher.Key PubViewKey;

        public Cipher.Key SecSpendKey;
        public Cipher.Key SecViewKey;

        public string Address;
    }

    /**
     * <summary>
     * The wallet class used for representing wallets on disk. <br /><br />
     * 
     * If encrypted, secret keys will need to be regenerated during runtime.<br />
     * They will always be generated deterministically from the entropy.<br /><br />
     * 
     * Entropy is encrypted using AES-256-CBC with the AES-256 key generated<br />
     * from the passphrase's SHA-512 and PBKDF2, with salt being "discreet". <br /><br />
     * 
     * PBKDF2 will always run for 4096 rounds for now. 
     * </summary>
     * 
     * 
     */
    public class Wallet
    {
        /* UTXO data */
        public UTXO[] UTXOs;

        /* base wallet data */
        public string Coin = "discreet"; /* should always be discreet */
        public bool Encrypted;
        public string Label;
        public ulong Timestamp;
        public string Version = "0.1"; /* Discreet wallets are currently in their first version */

        /* randomness buffer used to generate wallet addresses; can be encrypted with AES-256-CBC */
        public byte[] Entropy;
        public uint EntropyLen; /* currently can be 128 or 256 bits */

        public WalletAddress[] Addresses;

        /* WIP */
    }
}
