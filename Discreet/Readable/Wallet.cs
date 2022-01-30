using Discreet.Common;
using Discreet.Common.Exceptions;
using Discreet.RPC.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Discreet.Readable
{
    public class Wallet: IReadable
    {
        public string Label { get; set; }
        public string CoinName { get; set; }
        public bool Encrypted { get; set; }
        public ulong Timestamp { get; set; }
        public string Version { get; set; }
        public string Entropy { get; set; }
        public uint EntropyLen { get; set; }
        public ulong EntropyChecksum { get; set; }
        public List<WalletAddress> Addresses { get; set; }

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
            Wallet wallet = JsonSerializer.Deserialize<Wallet>(json);
            Label = wallet.Label;
            CoinName = wallet.CoinName;
            Encrypted = wallet.Encrypted;
            Timestamp = wallet.Timestamp;
            Version = wallet.Version;
            Entropy = wallet.Entropy;
            EntropyLen = wallet.EntropyLen;
            EntropyChecksum = wallet.EntropyChecksum;
            Addresses = wallet.Addresses;
        }

        public Wallet(Wallets.Wallet obj)
        {
            FromObject(obj);
        }

        public Wallet(string json)
        {
            FromJSON(json);
        }

        public Wallet() { }

        public void FromObject<T>(object obj)
        {
            if (typeof(T) == typeof(Wallets.Wallet))
            {
                FromObject((Wallets.Wallet)obj);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(Wallet).FullName);
            }
        }

        public void FromObject(Wallets.Wallet obj)
        {
            /* one of the few times we will check-and-exception for Readable */
            if (obj == null) throw new Exception("Discreet.Readable.Wallet.FromObject: Wallet is null!");

            /* due to stateless requirements, we must exception here instead of calling Encrypt() */
            if (!obj.IsEncrypted) throw new Exception("Discreet.Readable.Wallet.FromObject: Wallet must be encrypted!");

            Label = obj.Label;
            CoinName = obj.CoinName;
            Encrypted = obj.Encrypted;
            Timestamp = obj.Timestamp;
            Version = obj.Version;

            if (Encrypted)
            {
                Entropy = Printable.Hexify(obj.EncryptedEntropy);
            }
            else
            {
                Entropy = Printable.Hexify(obj.Entropy);
            }
            
            EntropyLen = obj.EntropyLen;
            EntropyChecksum = obj.EntropyChecksum;

            if (obj.Addresses != null)
            {
                Addresses = new List<WalletAddress>(obj.Addresses.Length);

                for (int i = 0; i < obj.Addresses.Length; i++)
                {
                    Addresses.Add(new WalletAddress(obj.Addresses[i]));
                }
            }
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Wallets.Wallet))
            {
                return (T)ToObject();
            }
            else
            {
                throw new ReadableException(typeof(Wallet).FullName, typeof(T).FullName);
            }
        }

        public object ToObject()
        {
            Wallets.Wallet obj = new();

            obj.Label = Label;
            obj.CoinName = CoinName;
            obj.Encrypted = Encrypted;
            obj.Timestamp = Timestamp;
            obj.Version = Version;

            if (Encrypted)
            {
                obj.EncryptedEntropy = Printable.Byteify(Entropy);
            }
            else
            {
                obj.Entropy = Printable.Byteify(Entropy);
            }

            obj.EntropyLen = EntropyLen;
            obj.EntropyChecksum = EntropyChecksum;

            if (Addresses != null)
            {
                obj.Addresses = new Wallets.WalletAddress[Addresses.Count];

                for (int i = 0; i < Addresses.Count; i++)
                {
                    obj.Addresses[i] = (Wallets.WalletAddress)Addresses[i].ToObject();
                }
            }

            obj.IsEncrypted = true;

            return obj;
        }
        [RPCEndpoint(endpoint_name: "create_wallet")]
        public static Wallets.Wallet FromReadable(string json)
        {
            return (Wallets.Wallet)new Wallet(json).ToObject();
        }

        public static string ToReadable(Wallets.Wallet obj)
        {
            return new Wallet(obj).JSON();
        }
    }
}
