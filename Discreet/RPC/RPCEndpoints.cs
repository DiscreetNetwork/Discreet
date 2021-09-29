using Discreet.Cipher;
using Discreet.Coin;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.RPC
{
   public class RPCEndpoints
    {
       public string test_getStealthAddress()
        {
            return new StealthAddress(KeyOps.GeneratePubkey(), KeyOps.GeneratePubkey()).ToString(); 
        }

    }
}
