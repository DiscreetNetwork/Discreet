using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.RPC.Common;
using Discreet.Common;
using Discreet.Cipher;
using Discreet.Cipher.Mnemonics;

namespace Discreet.RPC.Endpoints
{
    public class SeedRecoveryEndpoints
    {
        public class GetWalletSeedRV
        {
            public string Seed { get; set;  }
            public string Mnemonic { get; set; }

            public GetWalletSeedRV() { }
        }

        [RPCEndpoint("get_wallet_seed", APISet.SEED_RECOVERY)]
        public static object GetWalletSeed(string label, string passphrase)
        {
            try
            {
                var _wallet = Network.Handler.GetHandler().daemon.wallets.Where(x => x.Label == label).FirstOrDefault();

                if (_wallet == null)
                {
                    return new RPCError($"could not get wallet with label {label}");
                }

                if (_wallet.Encrypted)
                {
                    byte[] entropyEncryptionKey = new byte[32];
                    byte[] passhash = SHA512.HashData(SHA512.HashData(Encoding.UTF8.GetBytes(passphrase)).Bytes).Bytes;
                    KeyDerivation.PBKDF2(entropyEncryptionKey, passhash, 64, new byte[] { 0x44, 0x69, 0x73, 0x63, 0x72, 0x65, 0x65, 0x74 }, 8, 4096, 32);

                    byte[] entropyChecksumFull = SHA256.HashData(SHA256.HashData(entropyEncryptionKey).Bytes).Bytes;

                    byte[] entropyChecksumBytes = new byte[8];
                    Array.Copy(entropyChecksumFull, entropyChecksumBytes, 8);

                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(entropyChecksumBytes);
                    }

                    ulong entropyChecksum = BitConverter.ToUInt64(entropyChecksumBytes);

                    if (entropyChecksum != _wallet.EntropyChecksum)
                    {
                        return new RPCError("Wrong passphrase");
                    }

                    (CipherObject cipherObj, byte[] encryptedEntropyBytes) = CipherObject.GetFromPrependedArray(entropyEncryptionKey, _wallet.EncryptedEntropy);
                    Mnemonic _mnemonic = new Mnemonic(AESCBC.Decrypt(encryptedEntropyBytes, cipherObj));

                    return new GetWalletSeedRV
                    {
                        Seed = Printable.Hexify(_mnemonic.GetEntropy()),
                        Mnemonic = _mnemonic.GetMnemonic()
                    };
                }

                Mnemonic mnemonic = new Mnemonic(_wallet.Entropy);

                return new GetWalletSeedRV
                {
                    Seed = Printable.Hexify(mnemonic.GetEntropy()),
                    Mnemonic = mnemonic.GetMnemonic()
                };
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletSeed failed: {ex.Message}");

                return new RPCError($"Could not recover seed for wallet {label}");
            }
        }

        public class GetSecretKeyPRV
        {
            public string Spend { get; set; }
            public string View { get; set; }
            public string MnemonicSpend { get; set; }
            public string MnemonicView { get; set; }

            public GetSecretKeyPRV() { }
        }

        public class GetSecretKeyTRV
        {
            public string Secret { get; set; }
            public string Mnemonic { get; set; }

            public GetSecretKeyTRV() { }
        }

        [RPCEndpoint("get_secret_key", APISet.SEED_RECOVERY)]
        public static object GetSecretKey(string label, string passphrase, string address)
        {
            try
            {
                var _wallet = Network.Handler.GetHandler().daemon.wallets.Where(x => x.Label == label).FirstOrDefault();

                if (_wallet == null)
                {
                    return new RPCError($"could not get wallet with label {label}");
                }

                var _walletAddress = _wallet.Addresses.Where(x => x.Address == address).FirstOrDefault();

                if (_walletAddress == null)
                {
                    return new RPCError($"could not find wallet address {address}");
                }

                byte[] _entropy;

                if (_wallet.Encrypted)
                {
                    byte[] entropyEncryptionKey = new byte[32];
                    byte[] passhash = SHA512.HashData(SHA512.HashData(Encoding.UTF8.GetBytes(passphrase)).Bytes).Bytes;
                    KeyDerivation.PBKDF2(entropyEncryptionKey, passhash, 64, new byte[] { 0x44, 0x69, 0x73, 0x63, 0x72, 0x65, 0x65, 0x74 }, 8, 4096, 32);

                    byte[] entropyChecksumFull = SHA256.HashData(SHA256.HashData(entropyEncryptionKey).Bytes).Bytes;

                    byte[] entropyChecksumBytes = new byte[8];
                    Array.Copy(entropyChecksumFull, entropyChecksumBytes, 8);

                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(entropyChecksumBytes);
                    }

                    ulong entropyChecksum = BitConverter.ToUInt64(entropyChecksumBytes);

                    if (entropyChecksum != _wallet.EntropyChecksum)
                    {
                        return new RPCError("Wrong passphrase");
                    }

                    (CipherObject cipherObj, byte[] encryptedEntropyBytes) = CipherObject.GetFromPrependedArray(entropyEncryptionKey, _wallet.EncryptedEntropy);
                    _entropy = AESCBC.Decrypt(encryptedEntropyBytes, cipherObj);
                }
                else
                {
                    _entropy = _wallet.Entropy;
                }

                if (_walletAddress.Type == 0)
                {
                    (CipherObject cipherObjSpend, byte[] encryptedSecSpendKeyBytes) = CipherObject.GetFromPrependedArray(_entropy, _walletAddress.EncryptedSecSpendKey);
                    byte[] unencryptedSpendKey = AESCBC.Decrypt(encryptedSecSpendKeyBytes, cipherObjSpend);

                    (CipherObject cipherObjView, byte[] encryptedSecViewKeyBytes) = CipherObject.GetFromPrependedArray(_entropy, _walletAddress.EncryptedSecViewKey);
                    byte[] unencryptedViewKey = AESCBC.Decrypt(encryptedSecViewKeyBytes, cipherObjView);

                    var _spend = new Mnemonic(unencryptedSpendKey);
                    var _view = new Mnemonic(unencryptedViewKey);

                    var _rv = new GetSecretKeyPRV
                    {
                        Spend = Printable.Hexify(_spend.GetEntropy()),
                        View = Printable.Hexify(_view.GetEntropy()),
                        MnemonicSpend = _spend.GetMnemonic(),
                        MnemonicView = _view.GetMnemonic(),
                    };

                    Array.Clear(unencryptedSpendKey, 0, unencryptedSpendKey.Length);
                    Array.Clear(unencryptedViewKey, 0, unencryptedViewKey.Length);

                    return _rv;
                }
                else if (_walletAddress.Type == 1)
                {
                    (CipherObject cipherObjSec, byte[] encryptedSecKeyBytes) = CipherObject.GetFromPrependedArray(_entropy, _walletAddress.EncryptedSecKey);
                    byte[] unencryptedSecKey = AESCBC.Decrypt(encryptedSecKeyBytes, cipherObjSec);

                    var _secret = new Mnemonic(unencryptedSecKey);

                    var _rv = new GetSecretKeyTRV
                    {
                        Secret = Printable.Hexify(_secret.GetEntropy()),
                        Mnemonic = _secret.GetMnemonic(),
                    };

                    Array.Clear(unencryptedSecKey, 0, unencryptedSecKey.Length);

                    return _rv;
                }
                else
                {
                    throw new Exception("Discreet.Wallets.WalletAddress: unknown wallet type " + _walletAddress.Type);
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletSeed failed: {ex.Message}");

                return new RPCError($"Could not recover seed for wallet {label}'s address {address}");
            }
        }
    }
}
