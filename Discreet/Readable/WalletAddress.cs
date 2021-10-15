using Discreet.Common;
using Discreet.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Discreet.Readable
{
    public class WalletAddress: IReadable
    {
        public string EncryptedSecSpendKey { get; set; }
        public string EncryptedSecViewKey { get; set; }
        public string PubSpendKey { get; set; }
        public string PubViewKey { get; set; }
        public string Address { get; set; }

        public List<int> UTXOs { get; set; }

        public string JSON()
        {
            return JsonSerializer.Serialize(this);
        }

        public override string ToString()
        {
            return JSON();
        }

        public void FromJSON(string json)
        {
            WalletAddress addr = JsonSerializer.Deserialize<WalletAddress>(json);
            EncryptedSecSpendKey = addr.EncryptedSecSpendKey;
            EncryptedSecViewKey = addr.EncryptedSecViewKey;
            PubSpendKey = addr.PubSpendKey;
            PubViewKey = addr.PubViewKey;
            Address = addr.Address;
            UTXOs = addr.UTXOs;
        }

        public WalletAddress(Wallets.WalletAddress obj)
        {
            FromObject(obj);
        }

        public WalletAddress(string json)
        {
            FromJSON(json);
        }

        public void FromObject<T>(T obj)
        {
            if (typeof(T) == typeof(Wallets.WalletAddress))
            {
                dynamic t = obj;
                FromObject((Wallets.WalletAddress)t);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(WalletAddress).FullName);
            }
        }

        public void FromObject(Wallets.WalletAddress obj)
        {
            EncryptedSecSpendKey = Printable.Hexify(obj.EncryptedSecSpendKey);
            EncryptedSecViewKey = Printable.Hexify(obj.EncryptedSecViewKey);
            PubSpendKey = obj.PubSpendKey.ToHex();
            PubViewKey = obj.PubViewKey.ToHex();
            Address = obj.Address;

            if (obj.UTXOs != null)
            {
                UTXOs = new List<int>(obj.UTXOs.Count);

                for (int i = 0; i < obj.UTXOs.Count; i++)
                {
                    UTXOs.Add(obj.UTXOs[i].OwnedIndex);
                }
            }
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Wallets.WalletAddress))
            {
                dynamic t = ToObject();
                return (T)t;
            }
            else
            {
                throw new ReadableException(typeof(WalletAddress).FullName, typeof(T).FullName);
            }

        }

        public Wallets.WalletAddress ToObject()
        {
            Wallets.WalletAddress obj = new();

            obj.EncryptedSecSpendKey = Printable.Byteify(EncryptedSecSpendKey);
            obj.EncryptedSecViewKey = Printable.Byteify(EncryptedSecViewKey);
            obj.PubSpendKey = Cipher.Key.FromHex(PubSpendKey);
            obj.PubViewKey = Cipher.Key.FromHex(PubViewKey);
            obj.Address = Address;

            /* this is the only Readable which relies on DB being formatted correctly. Can't win em all. */
            if (UTXOs != null)
            {
                obj.UTXOs = new List<Wallets.UTXO>(UTXOs.Count);

                DB.DB db = DB.DB.GetDB();

                for (int i = 0; i < UTXOs.Count; i++)
                {
                    obj.UTXOs[i] = db.GetWalletOutput(UTXOs[i]);
                }
            }

            obj.Encrypted = true;

            return obj;
        }

        public static Wallets.WalletAddress FromReadable(string json)
        {
            return new WalletAddress(json).ToObject();
        }

        public static string ToReadable(Wallets.WalletAddress obj)
        {
            return new WalletAddress(obj).JSON();
        }
    }
}
