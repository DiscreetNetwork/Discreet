using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Discreet.Cipher;
using Discreet.Coin;
using Discreet.Wallets.Models;

namespace Discreet.Wallets.Utilities
{
    public static class AccountUtil
    {
        public static void CheckAccountIntegrity(this Account account)
        {
            if (account.Encrypted) return;

            if (account.Type == (byte)AddressType.STEALTH)
            {
                var pubSpendKey = account.PubSpendKey;
                var pubViewKey = account.PubViewKey;
                var secSpendKey = account.SecSpendKey;
                var secViewKey = account.SecViewKey;

                if (!KeyOps.InMainSubgroup(ref pubSpendKey))
                {
                    throw new Exception("public spend key is not in main subgroup!");
                }

                if (!KeyOps.InMainSubgroup(ref pubViewKey))
                {
                    throw new Exception("public view key is not in main subgroup!");
                }

                if (!KeyOps.ScalarmultBase(ref secSpendKey).Equals(pubSpendKey))
                {
                    throw new Exception("spend key does not match public key!");
                }

                if (!KeyOps.ScalarmultBase(ref secViewKey).Equals(pubViewKey))
                {
                    throw new Exception("view key does not match public key!");
                }

                if (new StealthAddress(pubViewKey, pubSpendKey).ToString() != account.Address)
                {
                    throw new Exception("address string does not match with public keys!");
                }

                var _verifyAddress = new StealthAddress(account.Address).Verify();
                if (_verifyAddress != null)
                {
                    throw _verifyAddress;
                }
            }
            else if (account.Type == (byte)AddressType.TRANSPARENT)
            {
                var pubKey = account.PubKey;
                var secKey = account.SecKey;

                if (!KeyOps.InMainSubgroup(ref pubKey))
                {
                    throw new Exception("public key is not in main subgroup!");
                }

                if (!KeyOps.ScalarmultBase(ref secKey).Equals(pubKey))
                {
                    throw new Exception("secret key does not match public key!");
                }

                if (new TAddress(pubKey).ToString() != account.Address)
                {
                    throw new Exception("address string does not match with public key!");
                }

                var _verifyAddress = new TAddress(account.Address).Verify();
                if (_verifyAddress != null)
                {
                    throw _verifyAddress;
                }
            }
            else
            {
                throw new Exception("unknown wallet type " + account.Type);
            }

            foreach (UTXO utxo in account.UTXOs)
            {
                utxo.CheckUTXOIntegrity();
            }
        }

        public static bool TryCheckAccountIntegrity(this Account account)
        {
            try
            {
                account.CheckAccountIntegrity();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
