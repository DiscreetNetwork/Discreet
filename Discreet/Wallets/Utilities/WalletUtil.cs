using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Cipher;
using Discreet.Wallets.Models;

namespace Discreet.Wallets.Utilities
{
    public static class WalletUtil
    {
        public static int GetNumDeterministicStealthAccounts(this SQLiteWallet wallet)
        {
            int i = 0;

            wallet.Accounts.ForEach(x =>
            {
                if (x.Type == 0 && x.Deterministic) i++;
            });

            return i;
        }

        public static int GetNumDeterministicTransparentAccounts(this SQLiteWallet wallet)
        {
            int i = 0;

            wallet.Accounts.ForEach(x =>
            {
                if (x.Type == 1 && x.Deterministic) i++;
            });

            return i;
        }

        public static ((Key, Key), (Key, Key)) GetStealthAccountKeyPairs(this SQLiteWallet wallet)
        {
            if (wallet.IsEncrypted) throw new ArgumentException("wallet is encrypted");

            var hash = wallet.GetStealthAccountHashData(out int index);

            byte[] tmpspend = new byte[wallet.Entropy.Length + hash.Length + 9];
            byte[] tmpview = new byte[wallet.Entropy.Length + hash.Length + 8];
            byte[] indexBytes = BitConverter.GetBytes(index);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(indexBytes);
            }

            Array.Copy(wallet.Entropy, tmpspend, wallet.Entropy.Length);
            Array.Copy(hash, 0, tmpspend, wallet.Entropy.Length, hash.Length);
            Array.Copy(indexBytes, 0, tmpspend, wallet.Entropy.Length + hash.Length, 4);

            Array.Copy(tmpspend, tmpview, wallet.Entropy.Length + hash.Length + 4);

            Array.Copy(new byte[] { 0x73, 0x70, 0x65, 0x6E, 0x64 }, 0, tmpspend, wallet.Entropy.Length + hash.Length + 4, 5);
            Array.Copy(new byte[] { 0x76, 0x69, 0x65, 0x77 }, 0, tmpview, wallet.Entropy.Length + hash.Length + 4, 4);

            Key spend = new(new byte[32]);
            Key view = new(new byte[32]);

            HashOps.HashToScalar(ref spend, SHA256.HashData(tmpspend).Bytes, 32);
            HashOps.HashToScalar(ref view, SHA256.HashData(tmpview).Bytes, 32);

            Key pubSpend = KeyOps.ScalarmultBase(ref spend);
            Key pubView = KeyOps.ScalarmultBase(ref view);

            Array.Clear(indexBytes);
            Array.Clear(tmpspend);
            Array.Clear(tmpview);
            Array.Clear(hash);

            return ((spend, pubSpend), (view, pubView));
        }

        private static byte[] GetStealthAccountHashData(this SQLiteWallet wallet, out int index)
        {
            int i = 0;
            Account lastAccount = null;
            var hash = new byte[32];
            wallet.Accounts.ForEach(x =>
            {
                if (x.Deterministic && x.Type == 0)
                {
                    lastAccount = x;
                    i++;
                }
            });

            if (lastAccount != null)
            {
                byte[] tmp = new byte[lastAccount.SecSpendKey.bytes.Length + lastAccount.SecViewKey.bytes.Length];
                Array.Copy(lastAccount.SecSpendKey.bytes, tmp, lastAccount.SecSpendKey.bytes.Length);
                Array.Copy(lastAccount.SecViewKey.bytes, 0, tmp, lastAccount.SecSpendKey.bytes.Length, lastAccount.SecViewKey.bytes.Length);

                hash = SHA256.HashData(SHA256.HashData(tmp).Bytes).Bytes;
                Array.Clear(tmp);
            }

            index = i;
            return hash;
        }

        public static (Key, Key) GetTransparentAccountKeyPair(this SQLiteWallet wallet)
        {
            if (wallet.IsEncrypted) throw new ArgumentException("wallet is encrypted");
            
            var hash = wallet.GetTransparentAccountHashData(out int index);
            
            byte[] tmpsec = new byte[wallet.Entropy.Length + hash.Length + 15];
            byte[] indexBytes = BitConverter.GetBytes(index);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(indexBytes);
            }

            Array.Copy(wallet.Entropy, tmpsec, wallet.Entropy.Length);
            Array.Copy(hash, 0, tmpsec, wallet.Entropy.Length, hash.Length);
            Array.Copy(indexBytes, 0, tmpsec, wallet.Entropy.Length + hash.Length, 4);

            Array.Copy(new byte[] { 0x74, 0x72, 0x61, 0x6E, 0x73, 0x70, 0x61, 0x72, 0x65, 0x6E, 0x74 }, 0, tmpsec, wallet.Entropy.Length + hash.Length + 4, 11);

            Key sec = new(new byte[32]);

            HashOps.HashToScalar(ref sec, SHA256.HashData(tmpsec).Bytes, 32);

            Key pub = KeyOps.ScalarmultBase(ref sec);

            Array.Clear(indexBytes);
            Array.Clear(tmpsec);
            Array.Clear(hash);

            return (sec, pub);
        }

        private static byte[] GetTransparentAccountHashData(this SQLiteWallet wallet, out int index)
        {
            int i = 0;
            Account lastAccount = null;
            var hash = new byte[32];
            wallet.Accounts.ForEach(x =>
            {
                if (x.Deterministic && x.Type == 1)
                {
                    lastAccount = x;
                    i++;
                }
            });

            if (lastAccount != null)
            {
                hash = SHA256.HashData(SHA256.HashData(lastAccount.SecKey.bytes).Bytes).Bytes;
            }

            index = i;
            return hash;
        }
    }
}
