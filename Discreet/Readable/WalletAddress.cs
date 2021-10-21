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
        public byte Type { get; set; }

        public string EncryptedSecSpendKey { get; set; }
        public string EncryptedSecViewKey { get; set; }
        public string PubSpendKey { get; set; }
        public string PubViewKey { get; set; }
        public string Address { get; set; }

        public string EncryptedSecKey { get; set; }
        public string PubKey { get; set; }
        
        public ulong Balance { get; set; }

        public List<int> UTXOs { get; set; }

        public string JSON()
        {
            return JsonSerializer.Serialize(this, ReadableOptions.Options);
        }

        public override string ToString()
        {
            return JSON();
        }

        public void FromJSON(string json)
        {
            WalletAddress addr = JsonSerializer.Deserialize<WalletAddress>(json);
            Type = addr.Type;
            EncryptedSecSpendKey = addr.EncryptedSecSpendKey;
            EncryptedSecViewKey = addr.EncryptedSecViewKey;
            PubSpendKey = addr.PubSpendKey;
            PubViewKey = addr.PubViewKey;
            PubKey = addr.PubKey;
            EncryptedSecKey = addr.EncryptedSecKey;
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

        public WalletAddress() { }

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
            Type = obj.Type;
            if (obj.EncryptedSecSpendKey != null) EncryptedSecSpendKey = Printable.Hexify(obj.EncryptedSecSpendKey);
            if (obj.EncryptedSecViewKey != null) EncryptedSecViewKey = Printable.Hexify(obj.EncryptedSecViewKey);
            if (obj.PubSpendKey.bytes != null) PubSpendKey = obj.PubSpendKey.ToHex();
            if (obj.PubViewKey.bytes != null) PubViewKey = obj.PubViewKey.ToHex();
            if (obj.Address!= null && obj.Address != "") Address = obj.Address;

            if (obj.EncryptedSecKey != null) EncryptedSecKey = Printable.Hexify(obj.EncryptedSecKey);
            if (obj.PubKey.bytes != null) PubKey = obj.PubKey.ToHex();

            Balance = obj.Balance;

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

            if (EncryptedSecSpendKey != null && EncryptedSecSpendKey != "") obj.EncryptedSecSpendKey = Printable.Byteify(EncryptedSecSpendKey);
            if (EncryptedSecViewKey != null && EncryptedSecViewKey != "") obj.EncryptedSecViewKey = Printable.Byteify(EncryptedSecViewKey);
            if (PubSpendKey != null && PubSpendKey != "") obj.PubSpendKey = Cipher.Key.FromHex(PubSpendKey);
            if (PubViewKey != null && PubViewKey != "") obj.PubViewKey = Cipher.Key.FromHex(PubViewKey);
            if (Address != null && Address != "") obj.Address = Address;

            obj.SecSpendKey = new Cipher.Key(new byte[32]);
            obj.SecViewKey = new Cipher.Key(new byte[32]);
            obj.SecKey = new Cipher.Key(new byte[32]);

            if (EncryptedSecKey != null && EncryptedSecKey != "") obj.EncryptedSecKey = Printable.Byteify(EncryptedSecKey);
            if (PubKey != null && PubKey != "") obj.PubKey = Cipher.Key.FromHex(PubKey);

            obj.Balance = Balance;

            /* this is the only Readable which relies on DB being formatted correctly. Can't win em all. */
            obj.UTXOs = new List<Wallets.UTXO>(UTXOs.Count);

            if (UTXOs != null)
            {
                DB.DB db = DB.DB.GetDB();

                for (int i = 0; i < UTXOs.Count; i++)
                {
                    obj.UTXOs.Add(db.GetWalletOutput(UTXOs[i]));
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
            /* only exception we can expect from ToReadable based on object state; still stateless but ugly */
            if (!obj.Encrypted) throw new Exception("Cannot create readable from unencrypted wallet!");

            return new WalletAddress(obj).JSON();
        }
    }
}
