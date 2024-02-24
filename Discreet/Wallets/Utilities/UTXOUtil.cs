using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Cipher;
using Discreet.Wallets.Models;

namespace Discreet.Wallets.Utilities
{
    public static class UTXOUtil
    {
        public static void CheckUTXOIntegrity(this UTXO utxo)
        {
            if (utxo.Encrypted) return;

            if (utxo.Type == 0)
            {
                if (!KeyOps.ScalarmultBase(ref utxo.UXSecKey).Equals(utxo.UXKey))
                {
                    throw new Exception("ux secret key does not match public key!");
                }
            }
        }
    }
}
