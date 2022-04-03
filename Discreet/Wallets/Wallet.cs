using System;
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
using System.Threading.Tasks;
using System.Threading;

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

        public long LastSeenHeight = -1;
        public bool Synced = false;

        public WalletAddress[] Addresses;

        /* if loaded from file, this will be set. Otherwise it will default to WalletPath/Label.dis */
        public string WalletPath = null;

        private object locker = new object();

        public CancellationTokenSource syncer = default;

        /* default constructor */
        public Wallet() { }

        /* WIP */
        public Wallet(string label, string passphrase, uint bip39 = 24, bool encrypted = true, bool deterministic = true, uint numStealthAddresses = 1, uint numTransparentAddresses = 0, List<string> stealthAddressNames = null, List<string> transparentAddressNames = null)
        {
            Timestamp = (ulong)DateTime.Now.Ticks;
            Encrypted = encrypted;
            Label = label;

            if (bip39 != 12 && bip39 != 24)
            {
                throw new Exception($"Discreet.Wallet.Wallet: Cannot make wallet with seed phrase length {bip39} (only 12 and 24)");
            }

            if (stealthAddressNames != null && stealthAddressNames.Count != numStealthAddresses)
            {
                throw new Exception($"Discreet.Wallet.Wallet: Cannot make wallet with mismatching stealth labels: {numStealthAddresses} != {stealthAddressNames.Count}");
            }

            if (transparentAddressNames != null && transparentAddressNames.Count != numTransparentAddresses)
            {
                throw new Exception($"Discreet.Wallet.Wallet: Cannot make wallet with mismatching transparent labels: {numTransparentAddresses} != {transparentAddressNames.Count}");
            }

            EntropyLen = (bip39 == 12) ? (uint)16 : (uint)32;
            Entropy = Cipher.Randomness.Random(EntropyLen);

            WalletDB db = WalletDB.GetDB();

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
                    Addresses[i] = new WalletAddress(this, (byte)WalletType.PRIVATE, Entropy, hash, i);
                }
                else
                {
                    Addresses[i] = new WalletAddress(this, (byte)WalletType.PRIVATE, true);
                }

                if (stealthAddressNames != null)
                {
                    Addresses[i].Name = stealthAddressNames[i];
                }

                Addresses[i].Encrypt(Entropy);
            }

            hash = new byte[32];

            for (int i = (int)numStealthAddresses; i < numStealthAddresses + numTransparentAddresses; i++)
            {
                if (deterministic)
                {
                    Addresses[i] = new WalletAddress(this, (byte)WalletType.TRANSPARENT, Entropy, hash, i);
                }
                else
                {
                    Addresses[i] = new WalletAddress(this, (byte)WalletType.TRANSPARENT, true);
                }

                if (transparentAddressNames != null)
                {
                    Addresses[i].Name = transparentAddressNames[i - (int)numStealthAddresses];
                }

                Addresses[i].Encrypt(Entropy);
            }

            IsEncrypted = encrypted;

            Decrypt(passphrase);
        }

        public Wallet(string label, string passphrase, string mnemonic, bool encrypted = true, bool deterministic = true, uint bip39 = 24, uint numStealthAddresses = 1, uint numTransparentAddresses = 0, List<string> stealthAddressNames = null, List<string> transparentAddressNames = null)
        {
            Timestamp = (ulong)DateTime.Now.Ticks;
            Encrypted = encrypted;
            Label = label;

            if (stealthAddressNames != null && stealthAddressNames.Count != numStealthAddresses)
            {
                throw new Exception($"Discreet.Wallet.Wallet: Cannot make wallet with mismatching stealth labels: {numStealthAddresses} != {stealthAddressNames.Count}");
            }

            if (transparentAddressNames != null && transparentAddressNames.Count != numTransparentAddresses)
            {
                throw new Exception($"Discreet.Wallet.Wallet: Cannot make wallet with mismatching transparent labels: {numTransparentAddresses} != {transparentAddressNames.Count}");
            }

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
                    Addresses[i] = new WalletAddress(this, (byte)WalletType.PRIVATE, Entropy, hash, i);
                }
                else
                {
                    Addresses[i] = new WalletAddress(this, (byte)WalletType.PRIVATE, true);
                }

                if (stealthAddressNames != null)
                {
                    Addresses[i].Name = stealthAddressNames[i];
                }

                Addresses[i].Encrypt(Entropy);
            }

            hash = new byte[32];

            for (int i = (int)numStealthAddresses; i < numStealthAddresses + numTransparentAddresses; i++)
            {
                if (deterministic)
                {
                    Addresses[i] = new WalletAddress(this, (byte)WalletType.TRANSPARENT, Entropy, hash, i);
                }
                else
                {
                    Addresses[i] = new WalletAddress(this, (byte)WalletType.TRANSPARENT, true);
                }

                if (transparentAddressNames != null)
                {
                    Addresses[i].Name = transparentAddressNames[i - (int)numStealthAddresses];
                }

                Addresses[i].Encrypt(Entropy);
            }

            IsEncrypted = encrypted;

            Decrypt(passphrase);
        }

        /**
         * <summary>tries to add a new wallet of the specified type. </summary>
         * 
         */
        public WalletAddress AddWallet(bool deterministic, bool transparent, string name = null)
        {
            lock (locker)
            {
                if (IsEncrypted) return null;

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
                    WalletAddress newAddr = new WalletAddress(this, (byte)(transparent ? WalletType.TRANSPARENT : WalletType.PRIVATE), true);
                    newAddr.Name = name ?? "";
                    AddWallet(newAddr);
                    return newAddr;
                }

                if (index == 0)
                {
                    WalletAddress newAddr = new WalletAddress(this, (byte)(transparent ? WalletType.TRANSPARENT : WalletType.PRIVATE), Entropy, hash, index);
                    newAddr.Name = name ?? "";
                    AddWallet(newAddr);

                    return newAddr;
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

                    WalletAddress newAddr = new WalletAddress(this, (byte)(transparent ? WalletType.TRANSPARENT : WalletType.PRIVATE), Entropy, hash, index);
                    newAddr.Name = name ?? "";
                    AddWallet(newAddr);

                    Array.Clear(hash, 0, hash.Length);

                    return newAddr;
                }
            }
        }

        public void AddWallet(WalletAddress addr)
        {
            lock (locker)
            {
                WalletDB db = WalletDB.GetDB();

                lock (WalletDB.DBLock)
                {
                    db.AddWalletAddress(addr);
                }

                addr.Encrypt(Entropy); // populate encrypted fields
                addr.Decrypt(Entropy);

                WalletAddress[] addrs = new WalletAddress[Addresses.Length + 1];
                for (int i = 0; i < Addresses.Length; i++)
                {
                    addrs[i] = Addresses[i];
                }

                addrs[Addresses.Length] = addr;

                Addresses = addrs;

                Save();
            }
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

        public void MustEncrypt()
        {
            lock (locker)
            {
                foreach (var addr in Addresses)
                {
                    addr.MustEncrypt(Entropy);
                }

                Encrypt();

                syncer.Cancel();
            }
        }

        public bool TryDecrypt(string passphrase = null)
        {
            if (!IsEncrypted) return true;

            try
            {
                lock (locker)
                {
                    if (Encrypted)
                    {
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
                    }

                    for (int i = 0; i < Addresses.Length; i++)
                    {
                        Addresses[i].Decrypt(Entropy);

                        Addresses[i].CheckIntegrity();
                    }

                    IsEncrypted = false;
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Discreet.Wallets.Wallet.TryDecrypt: could not decrypt wallet: {ex}");

                return false;
            }

            return true;
        }

        public void Decrypt(string passphrase = null)
        {
            if (!IsEncrypted) return;

            if (Encrypted)
            {
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
            }
            
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
        }

        public void ToFile(string path)
        {
            File.WriteAllText(path, Printable.Prettify(JSON()));
        }

        public static Wallet FromJSON(string json)
        {
            return Readable.Wallet.FromReadable(json);
        }

        public void ProcessBlock(Block block)
        {
            try
            {
                foreach (WalletAddress address in Addresses)
                {
                    address.ProcessBlock(block);
                }

                LastSeenHeight = block.Header.Height;

                WalletDB db = WalletDB.GetDB();

                lock (WalletDB.DBLock)
                {
                    db.SetWalletHeight(Label, LastSeenHeight);
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Wallet: ProcessBlock failed: {ex}");
            }
        }

        public bool CheckIntegrity()
        {
            return Addresses.All(x =>
            {
                try
                {
                    x.CheckIntegrity();

                    return true;
                }
                catch (Exception ex)
                {
                    Daemon.Logger.Error($"CheckIntegrity failed: {ex}");

                    return false;
                }
            });
        }

        public bool ChangeLabel(string label)
        {
            try
            {
                lock (locker)
                {
                    Label = label;
                }

                WalletDB db = WalletDB.GetDB();

                lock (WalletDB.DBLock)
                {
                    db.UpdateWallet(this);
                }

                return true;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Discreet.Wallets.Wallet.ChangeLabel: {ex}");

                return false;
            }
        }

        public byte[] Serialize()
        {
            MemoryStream _ms = new MemoryStream();

            Serialization.CopyData(_ms, Label);
            Serialization.CopyData(_ms, CoinName);
            Serialization.CopyData(_ms, Encrypted);
            Serialization.CopyData(_ms, Timestamp);
            Serialization.CopyData(_ms, Version);
            Serialization.CopyData(_ms, EntropyLen);
            Serialization.CopyData(_ms, Encrypted ? EncryptedEntropy : Entropy);
            Serialization.CopyData(_ms, EntropyChecksum);
            
            if (WalletPath != null)
            {
                Serialization.CopyData(_ms, WalletPath);
            }
            else
            {
                Serialization.CopyData(_ms, string.Empty);
            }

            Serialization.CopyData(_ms, Synced);

            Serialization.CopyData(_ms, Addresses.Length);

            foreach (var address in Addresses)
            {
                Serialization.CopyData(_ms, address.DBIndex);
            }

            return _ms.ToArray();
        }

        public void Deserialize(Stream s)
        {
            Label = Serialization.GetString(s);
            CoinName = Serialization.GetString(s);
            Encrypted = Serialization.GetBool(s);
            Timestamp = Serialization.GetUInt64(s);
            Version = Serialization.GetString(s);
            EntropyLen = Serialization.GetUInt32(s);

            byte[] _entropy = Serialization.GetBytes(s);

            if (Encrypted)
            {
                EncryptedEntropy = _entropy;
            }
            else
            {
                Entropy = _entropy;
            }

            EntropyChecksum = Serialization.GetUInt64(s);
            WalletPath = Serialization.GetString(s);

            if (WalletPath == "") WalletPath = null;

            Synced = Serialization.GetBool(s);

            var _numAddresses = Serialization.GetInt32(s);

            WalletDB db = WalletDB.GetDB();

            Addresses = new WalletAddress[_numAddresses];

            for (int i = 0; i < _numAddresses; i++)
            {
                Addresses[i] = db.GetWalletAddress(Serialization.GetInt32(s));
                Addresses[i].wallet = this;
            }

            IsEncrypted = true;
        }

        public void Save(bool _new)
        {
            var db = WalletDB.GetDB();

            lock (WalletDB.DBLock)
            {
                if (_new)
                {
                    db.AddWallet(this);
                }
                else
                {
                    db.UpdateWallet(this);
                }
            }
        }

        public void Save()
        {
            Save(false);
        }
    }
}
