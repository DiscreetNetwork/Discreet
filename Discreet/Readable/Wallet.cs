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
        public bool Locked { get; set; }
        public string Entropy { get; set; }
        public uint EntropyLen { get; set; }
        public ulong EntropyChecksum { get; set; }

        public long LastSeenHeight { get; set; }
        public bool Synced { get; set; }

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
            Locked = wallet.Locked;
            Entropy = wallet.Entropy;
            EntropyLen = wallet.EntropyLen;
            EntropyChecksum = wallet.EntropyChecksum;
            LastSeenHeight = wallet.LastSeenHeight;
            Synced = wallet.Synced;
            Addresses = wallet.Addresses;
        }

        public Wallet(WalletsLegacy.Wallet obj)
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
            if (typeof(T) == typeof(WalletsLegacy.Wallet))
            {
                FromObject((WalletsLegacy.Wallet)obj);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(Wallet).FullName);
            }
        }

        public void FromObject(WalletsLegacy.Wallet obj)
        {
            /* one of the few times we will check-and-exception for Readable */
            if (obj == null) throw new Exception("Discreet.Readable.Wallet.FromObject: Wallet is null!");

            /* due to stateless requirements, we must exception here instead of calling Encrypt() */
            //if (!obj.IsEncrypted) throw new Exception("Discreet.Readable.Wallet.FromObject: Wallet must be encrypted!");

            Label = obj.Label;
            CoinName = obj.CoinName;
            Encrypted = obj.Encrypted;
            Timestamp = obj.Timestamp;
            Version = obj.Version;
            Locked = obj.IsEncrypted;

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
            LastSeenHeight = obj.LastSeenHeight;
            Synced = obj.Synced;

            if (obj.Addresses != null)
            {
                Addresses = new List<WalletAddress>(obj.Addresses.Length);

                for (int i = 0; i < obj.Addresses.Length; i++)
                {
                    Addresses.Add(new WalletAddress(obj.Addresses[i], obj.Encrypted));
                }
            }
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(WalletsLegacy.Wallet))
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
            WalletsLegacy.Wallet obj = new();

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
            obj.LastSeenHeight = LastSeenHeight;
            obj.Synced = Synced;

            if (Addresses != null)
            {
                obj.Addresses = new WalletsLegacy.WalletAddress[Addresses.Count];

                for (int i = 0; i < Addresses.Count; i++)
                {
                    obj.Addresses[i] = (WalletsLegacy.WalletAddress)Addresses[i].ToObject();
                    obj.Addresses[i].wallet = obj;
                }
            }

            obj.IsEncrypted = true;

            return obj;
        }

        public static WalletsLegacy.Wallet FromReadable(string json)
        {
            return (WalletsLegacy.Wallet)new Wallet(json).ToObject();
        }

        public static string ToReadable(WalletsLegacy.Wallet obj)
        {
            return new Wallet(obj).JSON();
        }
    }
}
