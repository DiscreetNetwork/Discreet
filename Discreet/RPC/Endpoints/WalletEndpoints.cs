using Discreet.Cipher;
using Discreet.Coin;
using Discreet.RPC.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.RPC.Endpoints
{
    class WalletEndpoints
    {
        /* test RPC endpoints */
        [RPCEndpoint(endpoint_name: "getStealthAddressTest")]
        public static string test_getStealthAddress()
        {
            return new StealthAddress(KeyOps.GeneratePubkey(), KeyOps.GeneratePubkey()).ToString();
        }

        [RPCEndpoint(endpoint_name: "getStealthAddressTestDouble")]
        public static string also_test_getStealthAddress()
        {
            return new StealthAddress(KeyOps.GeneratePubkey(), KeyOps.GeneratePubkey()).ToString();
        }

        [RPCEndpoint(endpoint_name: "rpc_test_mutiply")]
        public static int TestFunction(int mult)
        {
            return 4 * mult;
        }

        /* endpoints used by the wallet GUI */
        [RPCEndpoint(endpoint_name: "createNewWallet")]
        public static object CreateNewWallet(string name, string passphrase, string seed = null, bool encrypted = true, bool deterministic = true, uint bip39 = 24, uint numStealthAddresses = 1, uint numTransparentAddresses = 0)
        {
            try
            {
                if (seed == "" || seed == null)
                {
                    Wallets.Wallet wallet = new Wallets.Wallet(name, passphrase, seed, encrypted, deterministic, bip39, numStealthAddresses, numTransparentAddresses);
                    return new Readable.Wallet(wallet);
                }
                else
                {
                    Wallets.Wallet wallet = new Wallets.Wallet(name, passphrase, bip39, encrypted, deterministic, numStealthAddresses, numTransparentAddresses);
                    return new Readable.Wallet(wallet);
                }
            }
            catch (Exception ex)
            {
                return new RPCError(-1, ex.Message);
            }
        }
    }
}
