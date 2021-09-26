using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using Discreet.Cipher;

namespace Discreet.Wallet
{
    /**
     * <summary>
     * Wallet addresses are generated either deterministically or randomly.
     * If deterministic, wallets will take in as a parameter the index, previous hash, and entropy.
     * </summary>
     */
    public class WalletAddress
    {
        public bool Encrypted;

        public Cipher.Key PubSpendKey;
        public Cipher.Key PubViewKey;

        public Cipher.Key SecSpendKey;
        public Cipher.Key SecViewKey;

        public byte[] EncryptedSecSpendKey;
        public byte[] EncryptedSecViewKey;

        public string Address;

        public WalletAddress()
        {
            SecSpendKey = Cipher.KeyOps.GenerateSeckey();
            SecViewKey = Cipher.KeyOps.GenerateSeckey();

            PubSpendKey = Cipher.KeyOps.ScalarmultBase(ref SecSpendKey);
            PubViewKey = Cipher.KeyOps.ScalarmultBase(ref SecViewKey);

            Address = new Coin.StealthAddress(PubViewKey, PubSpendKey).ToString();
        }

        /**
         * hash is set to zero first.
         * spendsk1 = ScalarReduce(SHA256(SHA256(Entropy||hash||index||"spend")))
         * viewsk1 = ScalarReduce(SHA256(SHA256(Entropy||hash||index||"view")))
         * hash = SHA256(SHA256(spendsk1||viewsk1))
         * ...
         */
        public WalletAddress(byte[] entropy, byte[] hash, int index)
        {
            byte[] tmpspend = new byte[entropy.Length + hash.Length + 9];
            byte[] tmpview = new byte[entropy.Length + hash.Length + 8];
            byte[] indexBytes = BitConverter.GetBytes(index);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(indexBytes);
            }

            Array.Copy(entropy, tmpspend, entropy.Length);
            Array.Copy(hash, 0, tmpspend, entropy.Length, hash.Length);
            Array.Copy(indexBytes, 0, tmpspend, entropy.Length + hash.Length, 4);

            Array.Copy(tmpspend, tmpview, entropy.Length + hash.Length + 4);

            Array.Copy(new byte[] { 0x73, 0x70, 0x65, 0x6E, 0x64 }, 0, tmpspend, entropy.Length + hash.Length + 4, 5);
            Array.Copy(new byte[] { 0x76, 0x69, 0x65, 0x77 }, 0, tmpview, entropy.Length + hash.Length + 4, 4);

            Cipher.HashOps.HashToScalar(ref SecSpendKey, Cipher.SHA256.HashData(tmpspend).Bytes, 32);
            Cipher.HashOps.HashToScalar(ref SecViewKey, Cipher.SHA256.HashData(tmpview).Bytes, 32);

            byte[] tmp = new byte[64];

            Array.Copy(SecSpendKey.bytes, tmp, 32);
            Array.Copy(SecViewKey.bytes, 0, tmp, 32, 32);

            byte[] newhash = Cipher.SHA256.HashData(Cipher.SHA256.HashData(tmp).Bytes).Bytes;

            Array.Copy(newhash, hash, 32);

            Array.Clear(tmpspend, 0, tmpspend.Length);
            Array.Clear(tmpview, 0, tmpview.Length);

            PubSpendKey = Cipher.KeyOps.ScalarmultBase(ref SecSpendKey);
            PubViewKey = Cipher.KeyOps.ScalarmultBase(ref SecViewKey);

            Address = new Coin.StealthAddress(PubViewKey, PubSpendKey).ToString();
        }

        public void MustEncrypt(byte[] key)
        {
            if (Encrypted) throw new Exception("Discreet.Wallet.WalletAddress.Encrypt: wallet is already encrypted!");

            Encrypt(key);
        }

        public void Encrypt(byte[] key)
        {
            if (Encrypted) return;

            CipherObject cipherObjSpend = AESCBC.GenerateCipherObject(key);
            byte[] encryptedSecSpendKeyBytes = AESCBC.Encrypt(SecSpendKey.bytes, cipherObjSpend);
            EncryptedSecSpendKey = cipherObjSpend.PrependIV(encryptedSecSpendKeyBytes);

            CipherObject cipherObjView = AESCBC.GenerateCipherObject(key);
            byte[] encryptedSecViewKeyBytes = AESCBC.Encrypt(SecSpendKey.bytes, cipherObjView);
            EncryptedSecViewKey = cipherObjView.PrependIV(encryptedSecViewKeyBytes);

            Array.Clear(SecSpendKey.bytes, 0, 32);
            Array.Clear(SecViewKey.bytes, 0, 32);

            Encrypted = true;
        }

        public void MustDecrypt(byte[] key)
        {
            if (!Encrypted) throw new Exception("Discreet.Wallet.WalletAddress.Decrypt: wallet is not encrypted!");

            Decrypt(key);
        }

        public void Decrypt(byte[] key)
        {
            if (!Encrypted) return;

            (CipherObject cipherObjSpend, byte[] encryptedSecSpendKeyBytes) = CipherObject.GetFromPrependedArray(key, EncryptedSecSpendKey);
            byte[] unencryptedSpendKey = AESCBC.Decrypt(encryptedSecSpendKeyBytes, cipherObjSpend);
            Array.Copy(unencryptedSpendKey, SecSpendKey.bytes, 32);

            (CipherObject cipherObjView, byte[] encryptedSecViewKeyBytes) = CipherObject.GetFromPrependedArray(key, EncryptedSecViewKey);
            byte[] unencryptedViewKey = AESCBC.Decrypt(encryptedSecViewKeyBytes, cipherObjView);
            Array.Copy(unencryptedViewKey, SecViewKey.bytes, 32);

            Array.Clear(unencryptedSpendKey, 0, unencryptedSpendKey.Length);
            Array.Clear(unencryptedViewKey, 0, unencryptedViewKey.Length);

            Array.Clear(EncryptedSecSpendKey, 0, EncryptedSecSpendKey.Length);
            Array.Clear(EncryptedSecViewKey, 0, EncryptedSecViewKey.Length);
        }

        public void EncryptDropKeys()
        {
            Array.Clear(SecSpendKey.bytes, 0, 32);
            Array.Clear(SecViewKey.bytes, 0, 32);

            Encrypted = true;
        }
    }

    /**
     * <summary>
     * The wallet class used for representing wallets on disk. <br /><br />
     * 
     * Secret keys are generated randomly and encrypted with AES-256-CBC with a<br />
     * master key (the entropy).<br /> <br />
     * 
     * Entropy is encrypted using AES-256-CBC with the AES-256 key generated<br />
     * from the passphrase's SHA-512 and PBKDF2, with salt being "Discreet". <br /><br />
     * 
     * PBKDF2 will always run for 4096 rounds for now. <br /><br />
     * </summary>
     * 
     * 
     */
    public class Wallet
    {
        /* UTXO data */
        public UTXO[] UTXOs;

        /* base wallet data */
        public string CoinName = "Discreet"; /* should always be Discreet */
        public bool Encrypted;
        public bool IsEncrypted;
        public string Label;
        public ulong Timestamp;
        public string Version = "0.1"; /* Discreet wallets are currently in their first version */

        /* randomness buffer used to generate wallet addresses; can be encrypted with AES-256-CBC */
        public byte[] Entropy;
        public byte[] EncryptedEntropy;
        public uint EntropyLen; /* currently can be 128 or 256 bits */

        public ulong EntropyChecksum;

        public WalletAddress[] Addresses;

        /* WIP */
        public Wallet(string label, string passphrase, bool encrypted = true, bool deterministic = true, uint bip39 = 24, uint numAddresses = 1)
        {
            Timestamp = (ulong)DateTime.Now.Ticks;
            Encrypted = encrypted;
            Label = label;

            if (bip39 != 12 && bip39 != 24)
            {
                throw new Exception($"Discreet.Wallet.Wallet: Cannot make wallet with seed phrase length {bip39} (only 12 and 24)");
            }

            EntropyLen = (bip39 == 12) ? (uint)16 : (uint)32;
            Entropy = Cipher.Randomness.Random(EntropyLen);

            if (Encrypted)
            {
                byte[] entropyEncryptionKey = new byte[32];
                byte[] passhash = Cipher.SHA512.HashData(Cipher.SHA512.HashData(Encoding.UTF8.GetBytes(passphrase)).Bytes).Bytes;
                Cipher.KeyDerivation.PBKDF2(entropyEncryptionKey, passhash, 64, new byte[] { 0x44, 0x69, 0x73, 0x63, 0x72, 0x65, 0x65, 0x74 }, 8, 4096, 32);

                CipherObject cipherObj = AESCBC.GenerateCipherObject(entropyEncryptionKey);

                byte[] entropyChecksumFull = Cipher.SHA256.HashData(Cipher.SHA256.HashData(entropyEncryptionKey).Bytes).Bytes;

                byte[] entropyChecksumBytes = new byte[8];
                Array.Copy(entropyChecksumFull, entropyChecksumBytes, 8);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(entropyChecksumBytes);
                }

                byte[] encryptedEntropyBytes = AESCBC.Encrypt(Entropy, cipherObj);
                EncryptedEntropy = cipherObj.PrependIV(encryptedEntropyBytes);

                EntropyChecksum = BitConverter.ToUInt64(entropyChecksumBytes);
            }

            Addresses = new WalletAddress[numAddresses];

            byte[] hash = new byte[32];

            for (int i = 0; i < numAddresses; i++)
            {
                if (deterministic)
                {
                    Addresses[i] = new WalletAddress(Entropy, hash, i);
                }
                else
                {
                    Addresses[i] = new WalletAddress();
                }

                Addresses[i].Encrypt(Entropy);
            }

            IsEncrypted = false;
        }

        public Wallet(string label, string passphrase, string mnemonic, bool encrypted = true, bool deterministic = true, uint bip39 = 24, uint numAddresses = 1)
        {
            Timestamp = (ulong)DateTime.Now.Ticks;
            Encrypted = encrypted;
            Label = label;

            Cipher.Mnemonics.Mnemonic Mnemonic = new Cipher.Mnemonics.Mnemonic(mnemonic);
            Entropy = Mnemonic.GetEntropy();
            EntropyLen = (uint)Entropy.Length;

            if (Encrypted)
            {

                byte[] entropyEncryptionKey = new byte[32];
                byte[] passhash = Cipher.SHA512.HashData(Cipher.SHA512.HashData(Encoding.UTF8.GetBytes(passphrase)).Bytes).Bytes;
                Cipher.KeyDerivation.PBKDF2(entropyEncryptionKey, passhash, 64, new byte[] { 0x44, 0x69, 0x73, 0x63, 0x72, 0x65, 0x65, 0x74 }, 8, 4096, 32);

                CipherObject cipherObj = AESCBC.GenerateCipherObject(entropyEncryptionKey);

                byte[] entropyChecksumFull = Cipher.SHA256.HashData(Cipher.SHA256.HashData(entropyEncryptionKey).Bytes).Bytes;

                byte[] entropyChecksumBytes = new byte[8];
                Array.Copy(entropyChecksumFull, entropyChecksumBytes, 8);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(entropyChecksumBytes);
                }

                byte[] encryptedEntropyBytes = AESCBC.Encrypt(Entropy, cipherObj);
                EncryptedEntropy = cipherObj.PrependIV(encryptedEntropyBytes);

                EntropyChecksum = BitConverter.ToUInt64(entropyChecksumBytes);
            }

            Addresses = new WalletAddress[numAddresses];

            byte[] hash = new byte[32];

            for (int i = 0; i < numAddresses; i++)
            {
                if (deterministic)
                {
                    Addresses[i] = new WalletAddress(Entropy, hash, i);
                }
                else
                {
                    Addresses[i] = new WalletAddress();
                }

                Addresses[i].Encrypt(Entropy);
            }
        }

        public string GetMnemonic()
        {
            Cipher.Mnemonics.Mnemonic mnemonic = new Cipher.Mnemonics.Mnemonic(Entropy);
            return mnemonic.GetMnemonic();
        }

        public void Encrypt()
        {
            if (!Encrypted) return;

            if (IsEncrypted) return;

            for (int i = 0; i < Addresses.Length; i++)
            {
                Addresses[i].EncryptDropKeys();
            }

            Array.Clear(Entropy, 0, (int)EntropyLen);

            IsEncrypted = true;
        }

        public void Decrypt(string passphrase)
        {
            if (!Encrypted) return;

            if (!IsEncrypted) return;

            

            byte[] entropyEncryptionKey = new byte[32];
            byte[] passhash = Cipher.SHA512.HashData(Cipher.SHA512.HashData(Encoding.UTF8.GetBytes(passphrase)).Bytes).Bytes;
            Cipher.KeyDerivation.PBKDF2(entropyEncryptionKey, passhash, 64, new byte[] { 0x44, 0x69, 0x73, 0x63, 0x72, 0x65, 0x65, 0x74 }, 8, 4096, 32);

            byte[] entropyChecksumFull = Cipher.SHA256.HashData(Cipher.SHA256.HashData(entropyEncryptionKey).Bytes).Bytes;

            byte[] entropyChecksumBytes = new byte[8];
            Array.Copy(entropyChecksumFull, entropyChecksumBytes, 8);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(entropyChecksumBytes);
            }

            ulong entropyChecksum = BitConverter.ToUInt64(entropyChecksumBytes);

            if (entropyChecksum != EntropyChecksum)
            {
                throw new Exception("Discreet.Wallet.Wallet.Decrypt: Wrong passphrase!");
            }

            (CipherObject cipherObj, byte[] encryptedEntropyBytes) = CipherObject.GetFromPrependedArray(entropyEncryptionKey, EncryptedEntropy);
            Entropy = AESCBC.Decrypt(encryptedEntropyBytes, cipherObj);
      
            for (int i = 0; i < Addresses.Length; i++)
            {
                Addresses[i].Decrypt(Entropy);
            }

            IsEncrypted = false;
        }

        public string JSON()
        {
            Encrypt();

            string rv = $"{{\"Label\":\"{Label}\",\"CoinName\":\"{CoinName}\",\"Encrypted\":{Encrypted},\"Timestamp\":{Timestamp},\"Version\":\"{Version}\",\"Entropy\":";

            if (Encrypted)
            {
                rv += $"\"{Coin.Printable.Hexify(EncryptedEntropy)}\",";
            }
            else
            {
                rv += $"\"{Coin.Printable.Hexify(Entropy)}\",";
            }

            rv += $"\"EntropyLen\":{EntropyLen},";

            if (Encrypted)
            {
                rv += $"\"EntropyChecksum\":{EntropyChecksum},";
            }

            rv += "\"Addresses\":[";

            for (int i = 0; i < Addresses.Length; i++)
            {
                rv += $"{{\"EncryptedSecSpendKey\":\"{Coin.Printable.Hexify(Addresses[i].EncryptedSecSpendKey)}\",\"EncryptedSecViewKey\":\"{Coin.Printable.Hexify(Addresses[i].EncryptedSecViewKey)}\",\"PubSpendKey\":\"{Addresses[i].PubSpendKey.ToHex()}\",\"PubViewKey\":\"{Addresses[i].PubViewKey.ToHex()}\",\"Address\":\"{Addresses[i].Address}\"}}";

                if (i < Addresses.Length - 1)
                {
                    rv += ",";
                }
            }

            rv += "]}";

            return rv;
        }
    }
}
