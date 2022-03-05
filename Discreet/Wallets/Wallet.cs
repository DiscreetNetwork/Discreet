﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using Discreet.Cipher;
using System.Text.Json;
using System.IO;
using System.Linq;
using Discreet.Coin;
using System.Runtime.InteropServices;
using Discreet.Common;

namespace Discreet.Wallets
{
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
     */
    public class Wallet
    {
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

        /* default constructor */
        public Wallet() { }

        /* WIP */
        public Wallet(string label, string passphrase, uint bip39 = 24, bool encrypted = true, bool deterministic = true, uint numStealthAddresses = 1, uint numTransparentAddresses = 0)
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

            Addresses = new WalletAddress[numStealthAddresses + numTransparentAddresses];

            byte[] hash = new byte[32];

            for (int i = 0; i < numStealthAddresses; i++)
            {
                if (deterministic)
                {
                    Addresses[i] = new WalletAddress((byte)WalletType.PRIVATE, Entropy, hash, i);
                }
                else
                {
                    Addresses[i] = new WalletAddress((byte)WalletType.PRIVATE, true);
                }

                Addresses[i].Encrypt(Entropy);
            }

            hash = new byte[32];

            for (int i = (int)numStealthAddresses; i < numStealthAddresses + numTransparentAddresses; i++)
            {
                if (deterministic)
                {
                    Addresses[i] = new WalletAddress((byte)WalletType.TRANSPARENT, Entropy, hash, i);
                }
                else
                {
                    Addresses[i] = new WalletAddress((byte)WalletType.TRANSPARENT, true);
                }

                Addresses[i].Encrypt(Entropy);
            }

            IsEncrypted = encrypted;

            Decrypt(passphrase);
        }

        public Wallet(string label, string passphrase, string mnemonic, bool encrypted = true, bool deterministic = true, uint bip39 = 24, uint numStealthAddresses = 1, uint numTransparentAddresses = 0)
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

            Addresses = new WalletAddress[numStealthAddresses + numTransparentAddresses];

            byte[] hash = new byte[32];

            for (int i = 0; i < numStealthAddresses; i++)
            {
                if (deterministic)
                {
                    Addresses[i] = new WalletAddress((byte)WalletType.PRIVATE, Entropy, hash, i);
                }
                else
                {
                    Addresses[i] = new WalletAddress((byte)WalletType.PRIVATE, true);
                }

                Addresses[i].Encrypt(Entropy);
            }

            hash = new byte[32];

            for (int i = (int)numStealthAddresses; i < numStealthAddresses + numTransparentAddresses; i++)
            {
                if (deterministic)
                {
                    Addresses[i] = new WalletAddress((byte)WalletType.TRANSPARENT, Entropy, hash, i);
                }
                else
                {
                    Addresses[i] = new WalletAddress((byte)WalletType.TRANSPARENT, true);
                }

                Addresses[i].Encrypt(Entropy);
            }

            IsEncrypted = encrypted;

            Decrypt(passphrase);
        }

        /**
         * <summary>tries to add a new wallet of the specified type. Returns true on success. </summary>
         * 
         */
        public bool AddWallet(bool deterministic, bool transparent)
        {
            if (IsEncrypted) return false;

            byte[] hash = new byte[32];
            WalletAddress lastSeenWallet = null;
            int index = 0;

            for (int i = 0; i < Addresses.Length; i++)
            {
                if (transparent && Addresses[i].Type == 1 && Addresses[i].Deterministic)
                {
                    lastSeenWallet = Addresses[i];
                    index++;
                }
                else if (!transparent && Addresses[i].Type == 0 && Addresses[i].Deterministic)
                {
                    lastSeenWallet = Addresses[i];
                    index++;
                }
            }

            if (!deterministic)
            {
                WalletAddress newAddr = new WalletAddress((byte)(transparent ? WalletType.TRANSPARENT : WalletType.PRIVATE), true);
                AddWallet(newAddr);
                return true;
            }

            if (index == 0)
            {
                WalletAddress newAddr = new WalletAddress((byte)(transparent ? WalletType.TRANSPARENT : WalletType.PRIVATE), Entropy, hash, index);
                AddWallet(newAddr);
            }
            else
            {
                if (transparent)
                {
                    byte[] hashingData = new byte[64];
                    Array.Copy(lastSeenWallet.SecSpendKey.bytes, hashingData, 32);
                    Array.Copy(lastSeenWallet.SecViewKey.bytes, 0, hashingData, 32, 32);
                    hash = Cipher.SHA256.HashData(Cipher.SHA256.HashData(hashingData).Bytes).Bytes;
                    Array.Clear(hashingData, 0, hashingData.Length);
                }
                else
                {
                    hash = Cipher.SHA256.HashData(Cipher.SHA256.HashData(lastSeenWallet.SecKey.bytes).Bytes).Bytes;
                }
                
                WalletAddress newAddr = new WalletAddress((byte)(transparent ? WalletType.TRANSPARENT : WalletType.PRIVATE), Entropy, hash, index);
                AddWallet(newAddr);

                Array.Clear(hash, 0, hash.Length);
            }

            return true;
        }

        private void AddWallet(WalletAddress addr)
        {
            WalletAddress[] addrs = new WalletAddress[Addresses.Length + 1];
            for (int i = 0; i < Addresses.Length; i++)
            {
                addrs[i] = Addresses[i];
            }

            addrs[Addresses.Length] = addr;

            Addresses = addrs;
        }

        public string GetMnemonic()
        {
            if (IsEncrypted)
            {
                throw new Exception("Discreet.Wallets.Wallet.GetMnemonic: wallet is encrypted");
            }

            Cipher.Mnemonics.Mnemonic mnemonic = new Cipher.Mnemonics.Mnemonic(Entropy);
            return mnemonic.GetMnemonic();
        }

        public void Encrypt()
        {
            if (IsEncrypted) return;

            for (int i = 0; i < Addresses.Length; i++)
            {
                Addresses[i].Encrypt(Entropy);
            }

            if (!Encrypted) return;

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
                throw new Exception("Discreet.Wallets.Wallet.Decrypt: Wrong passphrase!");
            }

            (CipherObject cipherObj, byte[] encryptedEntropyBytes) = CipherObject.GetFromPrependedArray(entropyEncryptionKey, EncryptedEntropy);
            Entropy = AESCBC.Decrypt(encryptedEntropyBytes, cipherObj);
      
            for (int i = 0; i < Addresses.Length; i++)
            {
                Addresses[i].Decrypt(Entropy);
            }

            IsEncrypted = false;
        }

        private string JSON()
        {
            // Encrypt();

            return Readable.Wallet.ToReadable(this);
        }

        /**
         * <summary>
         * This encrypts the wallet (if encrypted is set to true) and returns the resulting JSON string.
         * </summary>
         */
        public string ToJSON()
        {
            return JSON();
        }

        public static Wallet FromFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception($"Discreet.Wallets.Wallet.FromFile: no wallet found at path \"{path}\"");
            }

            string json = File.ReadAllText(path);

            return FromJSON(json);

            try
            {
                return FromJSON(json);
            }
            catch (Exception e)
            {
                throw new Exception($"Discreet.Wallets.Wallet.FromFile: an exception occurred when parsing data from \"{path}\": " + e.Message);
            }
        }

        public void ToFile(string path)
        {
            File.WriteAllText(path, Printable.Prettify(JSON()));
        }

        public static Wallet FromJSON(string json)
        {
            return Readable.Wallet.FromReadable(json);
        }

        public Transaction CreateTransaction(int walletIndex, StealthAddress to, ulong amount)
        {
            return CreateTransaction(walletIndex, new StealthAddress[] { to }, new ulong[] { amount });
        }

        public Transaction CreateTransaction(int walletIndex, StealthAddress[] to, ulong[] amount)
        {
            return CreateTransaction(walletIndex, to, amount, (byte)Config.TransactionVersions.BP_PLUS);
        }

        public Transaction CreateTransaction(int walletIndex, StealthAddress[] to, ulong[] amount, byte version)
        {
            if (Addresses.Length > walletIndex)
            {
                return Addresses[walletIndex].CreateTransaction(to, amount, (byte)Config.TransactionVersions.BP_PLUS);
            }

            throw new Exception("Discreet.Wallets.Wallet.CreateTransaction: walletIndex is outside of range!");
        }

        public void ProcessBlock(Block block)
        {
            if (IsEncrypted) throw new Exception("Do not call if wallet is encrypted!");

            if (block.Header.Version != 1)
            {
                var Coinbase = block.Transactions[0].ToPrivate();

                for (int i = 0; i < Addresses.Length; i++)
                {
                    if (Coinbase != null)
                    {
                        Key txKey = Coinbase.TransactionKey;
                        Key outputSecKey = KeyOps.DKSAPRecover(ref txKey, ref Addresses[i].SecViewKey, ref Addresses[i].SecSpendKey, i);
                        Key outputPubKey = KeyOps.ScalarmultBase(ref outputSecKey);

                        if (Coinbase.Outputs[0].UXKey.Equals(outputPubKey))
                        {
                            DB.DisDB db = DB.DisDB.GetDB();

                            (int index, UTXO utxo) = db.AddWalletOutput(Addresses[i], Coinbase.ToFull(), i, false, true);

                            utxo.OwnedIndex = index;

                            Addresses[i].UTXOs.Add(utxo);
                        }
                    }
                }
            }
            
            for (int i = block.Header.Version == 1 ? 0 : 1; i < block.Transactions.Length; i++)
            {
                ProcessTransaction(block.Transactions[i]);
            }

            ToFile(Path.Combine(Visor.VisorConfig.GetConfig().WalletPath, Label + ".dis"));
        }

        private void ProcessTransaction(FullTransaction transaction)
        {
            int numPOutputs = (transaction.Version == 4) ? transaction.NumPOutputs : ((transaction.Version == 3) ? 0 : transaction.NumOutputs);
            int numTOutputs = (transaction.Version == 4) ? transaction.NumTOutputs : ((transaction.Version == 3) ? transaction.NumOutputs : 0);

            for (int i = 0; i < transaction.NumPInputs; i++)
            {
                for (int j = 0; j < Addresses.Length; j++)
                {
                    if (Addresses[j].Type == (byte)AddressType.STEALTH)
                    {
                        for (int k = 0; k < Addresses[j].UTXOs.Count; k++)
                        {
                            if (Addresses[j].UTXOs[k].LinkingTag == transaction.PInputs[i].KeyImage)
                            {
                                Addresses[j].UTXOs[k].Decrypt(Addresses[j]);
                                Addresses[j].Balance -= Addresses[j].UTXOs[k].DecodedAmount;
                                Addresses[j].UTXOs.RemoveAt(k);
                            }
                        }
                    }
                    else if (Addresses[j].Type == (byte)AddressType.TRANSPARENT)
                    {
                        for (int k = 0; k < Addresses[j].UTXOs.Count; k++)
                        {
                            if (Addresses[j].UTXOs[k].TransactionSrc == transaction.TInputs[i].TransactionSrc && Addresses[j].UTXOs[k].Amount == transaction.TInputs[i].Amount && Addresses[j].Address == transaction.TInputs[i].Address.ToString())
                            {
                                Addresses[j].Balance -= Addresses[j].UTXOs[k].Amount;
                                Addresses[j].UTXOs.RemoveAt(k);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < numPOutputs; i++)
            {
                for (int addrIndex = 0; addrIndex < Addresses.Length; addrIndex++)
                {
                    if (Addresses[addrIndex].Type == (byte)AddressType.STEALTH)
                    {
                        Key txKey = transaction.TransactionKey;
                        Key outputSecKey = KeyOps.DKSAPRecover(ref txKey, ref Addresses[addrIndex].SecViewKey, ref Addresses[addrIndex].SecSpendKey, i);
                        Key outputPubKey = KeyOps.ScalarmultBase(ref outputSecKey);

                        for (int k = 0; k < Addresses[addrIndex].UTXOs.Count; k++)
                        {
                            if (Addresses[addrIndex].UTXOs[k].UXKey.Equals(transaction.POutputs[i].UXKey))
                            {
                                throw new Exception("Discreet.Wallets.Wallet.ProcessTransaction: duplicate UTXO being processed!");
                            }
                        }

                        if (transaction.POutputs[i].UXKey.Equals(outputPubKey))
                        {
                            Visor.Logger.Log("You received some Discreet!");
                            ProcessOutput(Addresses[addrIndex], transaction, i, addrIndex, false);
                        }
                    }
                }
            }

            for (int i = 0; i < numTOutputs; i++)
            {
                for (int addrIndex = 0; addrIndex < Addresses.Length; addrIndex++)
                {
                    if (Addresses[addrIndex].Type == (byte)AddressType.TRANSPARENT)
                    {
                        string address = transaction.TOutputs[i].Address.ToString();

                        if (Addresses[addrIndex].Address == address)
                        {
                            Visor.Logger.Log("You received some Discreet!");
                            ProcessOutput(Addresses[addrIndex], transaction, i, addrIndex, true);
                        }
                    }
                }
            }
        }

        private void ProcessOutput(WalletAddress addr, FullTransaction transaction, int i, int walletIndex, bool transparent)
        {
            DB.DisDB db = DB.DisDB.GetDB();

            lock (DB.DisDB.DBLock)
            {
                (int index, UTXO utxo) = db.AddWalletOutput(addr, transaction, i, transparent);

                utxo.OwnedIndex = index;

                Addresses[walletIndex].UTXOs.Add(utxo);

                Addresses[walletIndex].Balance += utxo.DecodedAmount;
            }
        }
    }
}
