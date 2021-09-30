using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using Discreet.Cipher;
using System.Text.Json;
using System.IO;
using System.Linq;
using Discreet.Coin;

namespace Discreet.Wallets
{
    public class ReadableWalletAddress
    {
        public string EncryptedSecSpendKey { get; set; }
        public string EncryptedSecViewKey { get; set; }
        public string PubSpendKey { get; set; }
        public string PubViewKey { get; set; }
        public string Address { get; set; }

        public List<int> UTXOs { get; set; }
    }

    public class ReadableWallet
    {
        public string Label { get; set; }
        public string CoinName { get; set; }
        public bool Encrypted { get; set; }
        public ulong Timestamp { get; set; }
        public string Version { get; set; }
        public string Entropy { get; set; }
        public uint EntropyLen { get; set; }
        public ulong EntropyChecksum { get; set; }
        public List<ReadableWalletAddress> Addresses { get; set; }
    }

    /**
     * <summary>
     * Wallet addresses are generated either deterministically or randomly.
     * If deterministic, wallets will take in as a parameter the index, previous hash, and entropy.
     * </summary>
     */
    public class WalletAddress
    {
        public bool Encrypted;

        public Key PubSpendKey;
        public Key PubViewKey;

        public Key SecSpendKey;
        public Key SecViewKey;

        public byte[] EncryptedSecSpendKey;
        public byte[] EncryptedSecViewKey;

        public string Address;

        public ulong Balance;

        /* UTXO data for the wallet. Stored in a local JSON database. */
        public List<UTXO> UTXOs;

        public WalletAddress(bool random)
        {
            if (random)
            {
                SecSpendKey = KeyOps.GenerateSeckey();
                SecViewKey = KeyOps.GenerateSeckey();

                PubSpendKey = Cipher.KeyOps.ScalarmultBase(ref SecSpendKey);
                PubViewKey = Cipher.KeyOps.ScalarmultBase(ref SecViewKey);

                Address = new StealthAddress(PubViewKey, PubSpendKey).ToString();

                UTXOs = new List<UTXO>();
            }
        }

        /* default constructor, used for falling back on JSON deserialize */
        public WalletAddress() { }

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

            Address = new StealthAddress(PubViewKey, PubSpendKey).ToString();

            UTXOs = new List<UTXO>();
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

            for (int i = 0; i < UTXOs.Count; i++)
            {
                UTXOs[i].Encrypt();
            }

            Balance = 0;

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

            for (int i = 0; i < UTXOs.Count; i++)
            {
                UTXOs[i].Decrypt(this);

                Balance += UTXOs[i].DecodedAmount;
            }

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
     */
    public class Wallet
    {
        /* UTXO data */
     

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
        public Wallet(string label, string passphrase, uint bip39 = 24, bool encrypted = true, bool deterministic = true, uint numAddresses = 1)
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
                    Addresses[i] = new WalletAddress(true);
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
                    Addresses[i] = new WalletAddress(true);
                }

                Addresses[i].Encrypt(Entropy);
            }

            IsEncrypted = false;
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
                Addresses[i].EncryptDropKeys();
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
            Encrypt();

            string rv = $"{{\"Label\":\"{Label}\",\"CoinName\":\"{CoinName}\",\"Encrypted\":{(Encrypted ? "true" : "false")},\"Timestamp\":{Timestamp},\"Version\":\"{Version}\",\"Entropy\":";

            if (Encrypted)
            {
                rv += $"\"{Printable.Hexify(EncryptedEntropy)}\",";
            }
            else
            {
                rv += $"\"{Printable.Hexify(Entropy)}\",";
            }

            rv += $"\"EntropyLen\":{EntropyLen},";

            if (Encrypted)
            {
                rv += $"\"EntropyChecksum\":{EntropyChecksum},";
            }

            rv += "\"Addresses\":[";

            for (int i = 0; i < Addresses.Length; i++)
            {
                rv += $"{{\"EncryptedSecSpendKey\":\"{Printable.Hexify(Addresses[i].EncryptedSecSpendKey)}\",\"EncryptedSecViewKey\":\"{Printable.Hexify(Addresses[i].EncryptedSecViewKey)}\",\"PubSpendKey\":\"{Addresses[i].PubSpendKey.ToHex()}\",\"PubViewKey\":\"{Addresses[i].PubViewKey.ToHex()}\",\"Address\":\"{Addresses[i].Address}\",\"UTXOs\":[";

                for (int j = 0; j < Addresses[i].UTXOs.Count; i++)
                {
                    rv += $"{Addresses[i].UTXOs[j].OwnedIndex}";

                    if (j < Addresses[i].UTXOs.Count - 1)
                    {
                        rv += ",";
                    }
                }

                rv += "]}";

                if (i < Addresses.Length - 1)
                {
                    rv += ",";
                }

                
            }

            rv += "]}";

            return rv;
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

            try
            {
                return Wallet.FromJSON(json);
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
            DB.DB db = DB.DB.GetDB();

            var jsonWallet = JsonSerializer.Deserialize<ReadableWallet>(json);

            Wallet wallet = new Wallet
            {
                Label = jsonWallet.Label,
                CoinName = jsonWallet.CoinName,
                Encrypted = jsonWallet.Encrypted,
                IsEncrypted = jsonWallet.Encrypted,
                Timestamp = jsonWallet.Timestamp,
                Version = jsonWallet.Version,
            };

            if (wallet.Encrypted)
            {
                wallet.EncryptedEntropy = Printable.Byteify(jsonWallet.Entropy);
                wallet.EntropyChecksum = jsonWallet.EntropyChecksum;
            }
            else
            {
                wallet.Entropy = Printable.Byteify(jsonWallet.Entropy);
            }

            wallet.Addresses = new WalletAddress[jsonWallet.Addresses.Count];

            for (int i = 0; i < wallet.Addresses.Length; i++)
            {
                wallet.Addresses[i] = new WalletAddress
                {
                    Encrypted = true,
                    EncryptedSecSpendKey = Printable.Byteify(jsonWallet.Addresses[i].EncryptedSecSpendKey),
                    EncryptedSecViewKey = Printable.Byteify(jsonWallet.Addresses[i].EncryptedSecViewKey),
                    PubSpendKey = new Key(Printable.Byteify(jsonWallet.Addresses[i].PubSpendKey)),
                    PubViewKey = new Key(Printable.Byteify(jsonWallet.Addresses[i].PubViewKey)),
                    Address = jsonWallet.Addresses[i].Address,
                    UTXOs = new List<UTXO>(),
                };

                for (int j = 0; j < wallet.Addresses[i].UTXOs.Count; j++)
                {
                    wallet.Addresses[i].UTXOs.Add(db.GetWalletOutput(jsonWallet.Addresses[i].UTXOs[j]));
                    wallet.Addresses[i].UTXOs[j].OwnedIndex = jsonWallet.Addresses[i].UTXOs[j];
                }

                if (!wallet.Encrypted)
                {
                    wallet.Addresses[i].Decrypt(wallet.Entropy);
                }

                /* sort by value */
                wallet.Addresses[i].UTXOs = wallet.Addresses[i].UTXOs.OrderBy(x => x.DecodedAmount).ToList();
            }

            return wallet;
        }

        public Transaction CreateTransaction(int walletIndex, StealthAddress[] to, ulong[] amount)
        {
            DB.DB db = DB.DB.GetDB();

            /* mahjick happens now. */
            WalletAddress addr = Addresses[walletIndex];

            int i = 0;

            if (IsEncrypted) throw new Exception("Discreet.Wallets.Wallet.CreateTransaction: Wallet is still encrypted!");

            ulong totalAmount = 0;
            for (i = 0; i < amount.Length; i++) totalAmount += amount[i];
            if (totalAmount > addr.Balance)
            {
                throw new Exception($"Discreet.Wallets.Wallet.CreateTransaction: sending amount is greater than wallet balance ({totalAmount} > {addr.Balance})");
            }

            /* Assemble pv and ps */
            Key[] pv = new Key[to.Length];
            Key[] ps = new Key[to.Length];

            for (i = 0; i < pv.Length; i++)
            {
                pv[i] = to[i].view;
                ps[i] = to[i].spend;
            }

            Key R = new Key(new byte[32]);
            Key r = new Key(new byte[32]);

            Key[] T = KeyOps.DKSAP(ref r, ref R, pv, ps);

            Transaction tx = new Transaction();

            /* get inputs */

            List<UTXO> inputs = new List<UTXO>();
            ulong neededAmount = 0;
            while (neededAmount < totalAmount)
            {
                neededAmount += addr.UTXOs[i].DecodedAmount;
                inputs.Add(addr.UTXOs[i]);
                i++;
            }

            tx.Version = (byte)Config.TransactionVersion;
            tx.NumInputs = (byte)inputs.Count;
            tx.NumOutputs = (byte)(to.Length + 1);
            tx.NumSigs = tx.NumInputs;

            /* for testnet, all tx fees are zero */
            tx.Fee = 0;

            /* assemble Extra */
            tx.ExtraLen = 34;
            tx.Extra = new byte[34];
            tx.Extra[0] = 1; // first byte is always 1.
            tx.Extra[0] = 0; // next byte indicates that no extra data besides TXKey is used in Extra.
            Array.Copy(R.bytes, 0, tx.Extra, 2, 32);

            /* assemble outputs */
            tx.Outputs = new TXOutput[to.Length + 1];

            Key[] gammas = new Key[to.Length + 1];

            for (i = 0; i < to.Length; i++)
            {
                tx.Outputs[i] = new TXOutput();
                tx.Outputs[i].UXKey = T[i];
                tx.Outputs[i].Commitment = new Key(new byte[32]);
                Key mask = KeyOps.GenCommitmentMask(ref r, ref to[i].view, i);
                KeyOps.GenCommitment(ref tx.Outputs[i].Commitment, ref mask, amount[i]);
                tx.Outputs[i].Amount = KeyOps.GenAmountMask(ref r, ref to[i].view, i, amount[i]);
                gammas[i] = mask;
            }

            /* assemble change output */
            tx.Outputs[i] = new TXOutput();
            tx.Outputs[i].UXKey = KeyOps.DKSAP(ref r, addr.PubViewKey, addr.PubSpendKey, i);
            tx.Outputs[i].Commitment = new Key(new byte[32]);
            Key rmask = KeyOps.GenCommitmentMask(ref r, ref addr.PubViewKey, i);
            KeyOps.GenCommitment(ref tx.Outputs[i].Commitment, ref rmask, amount[i]);
            tx.Outputs[i].Amount = KeyOps.GenAmountMask(ref r, ref addr.PubViewKey, i, neededAmount - totalAmount);
            gammas[i] = rmask;

            /* assemble range proof */
            Cipher.Bulletproof bp = Cipher.Bulletproof.Prove(amount, gammas);
            tx.RangeProof = new Coin.Bulletproof(bp);

            /* generate inputs and signatures */
            tx.Inputs = new TXInput[inputs.Count];
            tx.Signatures = new Coin.Triptych[inputs.Count];

            tx.PseudoOutputs = new Key[inputs.Count];

            Key[] blindingFactors = new Key[inputs.Count];

            Key sum = new Key(new byte[32]);

            for (i = 0; i < inputs.Count - 1; i++)
            {
                blindingFactors[i] = KeyOps.GenerateSeckey();
                tx.PseudoOutputs[i] = new Key(new byte[32]);
                KeyOps.GenCommitment(ref tx.PseudoOutputs[i], ref blindingFactors[i], inputs[i].DecodedAmount);
            }

            for (int k = 0; k < tx.Outputs.Length; k++)
            {
                KeyOps.AddKeys(ref sum, ref sum, ref gammas[k]);
            }

            Key dif = new Key(new byte[32]);

            for (int k = 0; k < blindingFactors.Length; k++)
            {
                KeyOps.ScalarAdd(ref dif, ref dif, ref blindingFactors[k]);
            }

            Key xm = new Key(new byte[32]);
            KeyOps.ScalarSub(ref xm, ref sum, ref dif);
            tx.PseudoOutputs[i] = new Key(new byte[32]);
            KeyOps.GenCommitment(ref tx.PseudoOutputs[i], ref xm, inputs[i].DecodedAmount);

            blindingFactors[i] = xm;

            for (i = 0; i < inputs.Count; i++)
            {
                /* get mixins */
                (TXOutput[] anonymitySet, int l) = db.GetMixins(inputs[i].Index);

                /* get ringsig params */
                Key[] M = new Key[64];
                Key[] P = new Key[64];
                Key C_offset = new Key(new byte[32]);
                KeyOps.SubKeys(ref C_offset, ref anonymitySet[l].Commitment, ref tx.PseudoOutputs[i]);
                Key sign_r = new Key(new byte[32]);
                KeyOps.DKSAPRecover(ref sign_r, ref R, ref addr.SecViewKey, ref addr.SecSpendKey, inputs[i].DecodeIndex);
                /* s = zt = xt - x't */
                Key sign_s = KeyOps.GenCommitmentMaskRecover(ref R, ref addr.SecViewKey, inputs[i].DecodeIndex);
                KeyOps.ScalarSub(ref sign_s, ref sign_s, ref blindingFactors[i]);

                /* signatures */
                Cipher.Triptych ringsig = Cipher.Triptych.Prove(M, P, C_offset, (uint)l, sign_r, sign_s, tx.SigningHash().ToKey());
                tx.Signatures[i] = new Coin.Triptych(ringsig);

                /* generate inputs */
                tx.Inputs[i] = new TXInput();
                tx.Inputs[i].Offsets = new uint[64];
                for (int k = 0; k < 64; k++)
                {
                    tx.Inputs[i].Offsets[k] = anonymitySet[k].Index;
                }

                tx.Inputs[i].KeyImage = tx.Signatures[i].J;
            }

            return tx;
        }
    }
}
