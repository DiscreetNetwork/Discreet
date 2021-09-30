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
    }
}
