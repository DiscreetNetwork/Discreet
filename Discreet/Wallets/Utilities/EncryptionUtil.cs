using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Discreet.Cipher;
using Discreet.Common;
using Discreet.Wallets.Models;

namespace Discreet.Wallets.Utilities
{
    public static class EncryptionUtil
    {
        public static bool CheckPassword(SHA256 checksum, string passphrase)
        {
            byte[] entropyEncryptionKey = new byte[32];
            byte[] passhash = SHA512.HashData(SHA512.HashData(Encoding.UTF8.GetBytes(passphrase)).Bytes).Bytes;
            KeyDerivation.PBKDF2(entropyEncryptionKey, passhash, 64, new byte[] { 0x44, 0x69, 0x73, 0x63, 0x72, 0x65, 0x65, 0x74 }, 8, 4096, 32);

            SHA256 entropyChecksum = SHA256.HashData(SHA256.HashData(entropyEncryptionKey).Bytes);
            var rv = false;

            if (entropyChecksum == checksum)
            {
                rv = true;
            }

            Array.Clear(entropyEncryptionKey);
            Array.Clear(entropyChecksum.Bytes);
            Array.Clear(passhash);

            return rv;
        }

        public static byte[] DecryptEntropy(this byte[] encryptedEntropy, SHA256 checksum, string passphrase)
        {
            byte[] entropyEncryptionKey = new byte[32];
            byte[] passhash = SHA512.HashData(SHA512.HashData(Encoding.UTF8.GetBytes(passphrase)).Bytes).Bytes;
            KeyDerivation.PBKDF2(entropyEncryptionKey, passhash, 64, new byte[] { 0x44, 0x69, 0x73, 0x63, 0x72, 0x65, 0x65, 0x74 }, 8, 4096, 32);

            SHA256 entropyChecksum = SHA256.HashData(SHA256.HashData(entropyEncryptionKey).Bytes);

            if (entropyChecksum != checksum)
            {
                throw new Exception("Wrong passphrase!");
            }

            (CipherObject cipherObj, byte[] encryptedEntropyBytes) = CipherObject.GetFromPrependedArray(entropyEncryptionKey, encryptedEntropy);
            var rv = AESCBC.Decrypt(encryptedEntropyBytes, cipherObj);
            Array.Clear(entropyEncryptionKey);
            Array.Clear(passhash);

            return rv;
        }

        public static byte[] EncryptEntropy(this byte[] entropy, string passphrase, out SHA256 checksum)
        {
            byte[] entropyEncryptionKey = new byte[32];
            byte[] passhash = SHA512.HashData(SHA512.HashData(Encoding.UTF8.GetBytes(passphrase)).Bytes).Bytes;
            KeyDerivation.PBKDF2(entropyEncryptionKey, passhash, 64, new byte[] { 0x44, 0x69, 0x73, 0x63, 0x72, 0x65, 0x65, 0x74 }, 8, 4096, 32);

            CipherObject cipherObj = AESCBC.GenerateCipherObject(entropyEncryptionKey);

            checksum = SHA256.HashData(SHA256.HashData(entropyEncryptionKey).Bytes);
            byte[] encryptedEntropyBytes = AESCBC.Encrypt(entropy, cipherObj);

            Array.Clear(entropyEncryptionKey);
            Array.Clear(passhash);

            return cipherObj.PrependIV(encryptedEntropyBytes);
        }

        public static void EncryptAccountPrivateKeys(this Account account, byte[] encryptionKey)
        {
            if (account.EncryptedSecKeyMaterial != null)
            {
                if (account.Type == 0)
                {
                    Array.Clear(account.SecSpendKey.bytes);
                    Array.Clear(account.SecViewKey.bytes);
                }
                else if (account.Type == 1)
                {
                    Array.Clear(account.SecKey.bytes);
                }
                else
                {
                    throw new FormatException();
                }

                return;
            }

            byte[] secKeyMaterial;
            if (account.Type == 0)
            {
                secKeyMaterial = new byte[account.SecSpendKey.bytes.Length + account.SecViewKey.bytes.Length];
                Array.Copy(account.SecSpendKey.bytes, secKeyMaterial, account.SecSpendKey.bytes.Length);
                Array.Copy(account.SecViewKey.bytes, 0, secKeyMaterial, account.SecSpendKey.bytes.Length, account.SecViewKey.bytes.Length);

                Array.Clear(account.SecSpendKey.bytes);
                Array.Clear(account.SecViewKey.bytes);
            }
            else if (account.Type == 1)
            {
                secKeyMaterial = new byte[account.SecKey.bytes.Length];
                Array.Copy(account.SecKey.bytes, secKeyMaterial, account.SecKey.bytes.Length);

                Array.Clear(account.SecKey.bytes);
            }
            else
            {
                throw new FormatException(nameof(account));
            }

            CipherObject cipherObjSecKeyMaterial = AESCBC.GenerateCipherObject(encryptionKey);
            byte[] encryptedSecKeyMaterial = AESCBC.Encrypt(secKeyMaterial, cipherObjSecKeyMaterial);
            account.EncryptedSecKeyMaterial = cipherObjSecKeyMaterial.PrependIV(encryptedSecKeyMaterial);

            Array.Clear(encryptedSecKeyMaterial);
            Array.Clear(secKeyMaterial);
        }

        public static void DecryptAccountPrivateKeys(this Account account, byte[] encryptionKey)
        {
            (Key spend, Key view, Key sec) = account.DecryptPrivateKeys(encryptionKey);

            if (account.Type == 0)
            {
                account.SecSpendKey = spend;
                account.SecViewKey = view;
            }
            else if (account.Type == 1)
            {
                account.SecKey = sec;
            }
            else
            {
                throw new FormatException(nameof(account));
            }
        }

        public static (Key Spend, Key View, Key Sec) DecryptPrivateKeys(this Account account, byte[] encryptionKey)
        {
            (CipherObject cipherObjSecKeyMaterial, byte[] encryptedSecKeyMaterial) = CipherObject.GetFromPrependedArray(encryptionKey, account.EncryptedSecKeyMaterial);
            byte[] secKeyMaterial = AESCBC.Decrypt(encryptedSecKeyMaterial, cipherObjSecKeyMaterial);

            if (account.Type == 0)
            {
                var spend = new Key(new byte[32]);
                var view = new Key(new byte[32]);
                Array.Copy(secKeyMaterial, spend.bytes, view.bytes.Length);
                Array.Copy(secKeyMaterial, spend.bytes.Length, view.bytes, 0, view.bytes.Length);

                Array.Clear(encryptedSecKeyMaterial);
                Array.Clear(secKeyMaterial);

                return (spend, view, default);
            }
            else if (account.Type == 1)
            {
                var sec = new Key(new byte[32]);
                Array.Copy(secKeyMaterial, sec.bytes, sec.bytes.Length);

                Array.Clear(encryptedSecKeyMaterial);
                Array.Clear(secKeyMaterial);

                return (default, default, sec);
            }
            else
            {
                throw new FormatException(nameof(account));
            }
        }

        public static void EncryptUTXOs(this Account account)
        {
            account.UTXOs.AsParallel().AsUnordered().ForAll(utxo =>
            {
                if (utxo.Type == 0)
                {
                    utxo.DecodedAmount = 0;
                    Array.Clear(utxo.UXSecKey.bytes);
                }

                utxo.Encrypted = true;
            });
        }

        public static void DecryptUTXOs(this Account account)
        {
            account.UTXOs.AsParallel().AsUnordered().ForAll(utxo =>
            {
                if (utxo.Type == 0)
                {
                    Key txk = utxo.TransactionKey.Value;

                    if (utxo.IsCoinbase)
                    {
                        utxo.DecodedAmount = utxo.Amount;
                    }
                    else
                    {
                        utxo.DecodedAmount = KeyOps.GenAmountMaskRecover(ref txk, ref account.SecViewKey, utxo.DecodeIndex ?? 0, utxo.Amount);
                    }

                    utxo.UXSecKey = new(new byte[32]);
                    KeyOps.DKSAPRecover(ref utxo.UXSecKey, ref txk, ref account.SecViewKey, ref account.SecSpendKey, utxo.DecodeIndex ?? 0);

                    if (utxo.LinkingTag == Key.Z || utxo.LinkingTag == default)
                    {
                        Key lt = new(new byte[32]);
                        KeyOps.GenerateLinkingTag(ref lt, ref utxo.UXSecKey);
                        utxo.LinkingTag = lt;
                    }
                }
                else if (utxo.Type == 1)
                {
                    utxo.DecodedAmount = utxo.Amount;
                }
                else
                {
                    throw new FormatException();
                }

                if (utxo.Account == null) utxo.Account = account;

                utxo.Encrypted = false;
            });
        }

        private static byte[] GetTxHistoryEncryptionKey(this Account account)
        {
            if (account.Encrypted) throw new FormatException();

            if (account.Type == 0)
            {
                byte[] _key = new byte[account.SecSpendKey.bytes.Length + account.SecViewKey.bytes.Length];
                Array.Copy(account.SecSpendKey.bytes, 0, _key, 0, account.SecSpendKey.bytes.Length);
                Array.Copy(account.SecViewKey.bytes, 0, _key, account.SecSpendKey.bytes.Length, account.SecViewKey.bytes.Length);

                byte[] _rv = SHA256.HashData(SHA256.HashData(_key).Bytes).Bytes;

                Array.Clear(_key);

                return _rv;
            }
            else if (account.Type == 1)
            {
                return SHA256.HashData(SHA256.HashData(account.SecKey.bytes).Bytes).Bytes;
            }
            else
            {
                throw new FormatException();
            }
        } 

        public static void EncryptHistoryTxs(this Account account)
        {
            foreach (var tx in account.TxHistory)
            {
                if (tx.EncryptedRawData == null)
                {
                    using var ms = new MemoryStream();
                    ms.Write(tx.TxID.Bytes);
                    Serialization.CopyData(ms, tx.Timestamp);
                    Serialization.CopyData(ms, tx.Inputs.Count);
                    tx.Inputs.ForEach(to =>
                    {
                        Serialization.CopyData(ms, to.Address);
                        Serialization.CopyData(ms, to.Amount);
                    });
                    Serialization.CopyData(ms, tx.Outputs.Count);
                    tx.Outputs.ForEach(to =>
                    {
                        Serialization.CopyData(ms, to.Address);
                        Serialization.CopyData(ms, to.Amount);
                    });

                    var txRawDataEncryptionKey = account.GetTxHistoryEncryptionKey();
                    CipherObject cipherObjTxRawData = AESCBC.GenerateCipherObject(txRawDataEncryptionKey);
                    var txEncryptedRawData = AESCBC.Encrypt(ms.ToArray(), cipherObjTxRawData);
                    tx.EncryptedRawData = cipherObjTxRawData.PrependIV(txEncryptedRawData);
                    Array.Clear(txRawDataEncryptionKey);
                }

                tx.TxID = default;
                tx.Timestamp = 0;
                tx.Inputs.ForEach(to =>
                {
                    to.Address = string.Empty;
                    to.Amount = 0;
                });
                tx.Outputs.ForEach(to =>
                {
                    to.Address = string.Empty;
                    to.Amount = 0;
                });
            }
        }

        public static void EncryptHistoryTxs(this Account account, IEnumerable<HistoryTx> txs)
        {
            txs.AsParallel().AsUnordered().ForAll(tx =>
            {
                if (tx.EncryptedRawData == null)
                {
                    using var ms = new MemoryStream();
                    ms.Write(tx.TxID.Bytes);
                    Serialization.CopyData(ms, tx.Timestamp);
                    Serialization.CopyData(ms, tx.Inputs.Count);
                    tx.Inputs.ForEach(to =>
                    {
                        Serialization.CopyData(ms, to.Address);
                        Serialization.CopyData(ms, to.Amount);
                    });
                    Serialization.CopyData(ms, tx.Outputs.Count);
                    tx.Outputs.ForEach(to =>
                    {
                        Serialization.CopyData(ms, to.Address);
                        Serialization.CopyData(ms, to.Amount);
                    });

                    var txRawDataEncryptionKey = account.GetTxHistoryEncryptionKey();
                    CipherObject cipherObjTxRawData = AESCBC.GenerateCipherObject(txRawDataEncryptionKey);
                    var txEncryptedRawData = AESCBC.Encrypt(ms.ToArray(), cipherObjTxRawData);
                    tx.EncryptedRawData = cipherObjTxRawData.PrependIV(txEncryptedRawData);
                    Array.Clear(txRawDataEncryptionKey);
                }
            });
        }

        public static void DecryptHistoryTxs(this Account account) => DecryptHistoryTxs(account, account.TxHistory);

        public static void DecryptHistoryTxs(this Account account, IEnumerable<HistoryTx> txs)
        {
            var txRawDataEncryptionKey = account.GetTxHistoryEncryptionKey();
            txs.AsParallel().AsUnordered().ForAll(tx =>
            {
                (var cipherObjTxRawData, var txEncryptedRawData) = CipherObject.GetFromPrependedArray(txRawDataEncryptionKey, tx.EncryptedRawData);
                var txRawData = AESCBC.Decrypt(txEncryptedRawData, cipherObjTxRawData);

                using var ms = new MemoryStream(txRawData);
                tx.TxID = new SHA256(ms);
                tx.Timestamp = Serialization.GetInt64(ms);

                tx.Inputs = new();
                var numInputs = Serialization.GetInt32(ms);
                for (int i = 0; i < numInputs; i++)
                {
                    var to = new HistoryTxOutput();
                    to.Address = Serialization.GetString(ms);
                    to.Amount = Serialization.GetUInt64(ms);
                    tx.Inputs.Add(to);
                }

                tx.Outputs = new();
                var numOutputs = Serialization.GetInt32(ms);
                for (int i = 0; i < numOutputs; i++)
                {
                    var to = new HistoryTxOutput();
                    to.Address = Serialization.GetString(ms);
                    to.Amount = Serialization.GetUInt64(ms);
                    tx.Outputs.Add(to);
                }

                Array.Clear(txEncryptedRawData);
            });

            Array.Clear(txRawDataEncryptionKey);
        }
    }
}
