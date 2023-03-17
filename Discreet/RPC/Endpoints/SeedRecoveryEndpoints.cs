using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.RPC.Common;
using Discreet.Common;
using Discreet.Cipher;
using Discreet.Cipher.Mnemonics;
using Discreet.Wallets;

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
                (var seed, var mnemonic) = SQLiteWallet.GetMnemonic(label, passphrase);

                if (seed != null && mnemonic != null)
                {
                    return new GetWalletSeedRV
                    {
                        Seed = seed,
                        Mnemonic = mnemonic.GetMnemonic()
                    };
                }
                else return new RPCError("wrong passphrase");
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletSeed failed: {ex.Message}", ex);

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
                (var spend, var view, var sec) = SQLiteWallet.GetAccountPrivateKeys(label, address, passphrase);

                if (spend == null && view == null && sec == null) return new RPCError("wrong passphrase");
                else
                {
                    if (sec == null)
                    {
                        return new GetSecretKeyPRV
                        {
                            Spend = Printable.Hexify(spend.GetEntropy()),
                            View = Printable.Hexify(view.GetEntropy()),
                            MnemonicSpend = spend.GetMnemonic(),
                            MnemonicView = view.GetMnemonic()
                        };
                    }
                    else
                    {
                        return new GetWalletSeedRV
                        {
                            Seed = Printable.Hexify(sec.GetEntropy()),
                            Mnemonic = sec.GetMnemonic()
                        };
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetSecretKey failed: {ex.Message}", ex);

                return new RPCError($"Could not recover seed for wallet {label}'s address {address}");
            }
        }
    }
}
