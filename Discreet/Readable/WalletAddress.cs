using Discreet.Coin;
using Discreet.Common;
using Discreet.Common.Exceptions;
using Discreet.WalletsLegacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Discreet.Readable
{
    public class WalletAddress: IReadable
    {
        public string Name { get; set; }
        public byte Type { get; set; }

        public bool Deterministic { get; set; }

        public string EncryptedSecSpendKey { get; set; }
        public string EncryptedSecViewKey { get; set; }
        public string PubSpendKey { get; set; }
        public string PubViewKey { get; set; }
        public string Address { get; set; }

        public string EncryptedSecKey { get; set; }
        public string PubKey { get; set; }
        
        public bool Synced { get; set; }
        public bool Syncer { get; set; }
        public long LastSeenHeight { get; set; }

        public ulong Balance { get; set; }

        public List<int> UTXOs { get; set; }

        public List<int> TxHistory { get; set; }

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
            Deterministic = addr.Deterministic;
            EncryptedSecSpendKey = addr.EncryptedSecSpendKey;
            EncryptedSecViewKey = addr.EncryptedSecViewKey;
            PubSpendKey = addr.PubSpendKey;
            PubViewKey = addr.PubViewKey;
            PubKey = addr.PubKey;
            EncryptedSecKey = addr.EncryptedSecKey;
            Address = addr.Address;
            LastSeenHeight = addr.LastSeenHeight;
            Synced = addr.Synced;
            Syncer = addr.Syncer;
            UTXOs = addr.UTXOs;
            TxHistory = addr.TxHistory;
            Name = addr.Name;
        }

        public WalletAddress(WalletsLegacy.WalletAddress obj)
        {
            FromObject(obj, false);
        }

        public WalletAddress(WalletsLegacy.WalletAddress obj, bool encrypted)
        {
            FromObject(obj, encrypted);
        }

        public WalletAddress(string json)
        {
            FromJSON(json);
        }

        public WalletAddress() { }

        public void FromObject<T>(object obj)
        {
            if (typeof(T) == typeof(WalletsLegacy.WalletAddress))
            {
                FromObject((WalletsLegacy.WalletAddress)obj, false);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(WalletAddress).FullName);
            }
        }

        public void FromObject(WalletsLegacy.WalletAddress obj, bool encrypted)
        {
            Name = obj.Name;
            Type = obj.Type;
            Deterministic = obj.Deterministic;
            if (obj.EncryptedSecSpendKey != null) EncryptedSecSpendKey = Printable.Hexify(obj.EncryptedSecSpendKey);
            if (obj.EncryptedSecViewKey != null) EncryptedSecViewKey = Printable.Hexify(obj.EncryptedSecViewKey);
            if (obj.PubSpendKey.bytes != null) PubSpendKey = obj.PubSpendKey.ToHex();
            if (obj.PubViewKey.bytes != null) PubViewKey = obj.PubViewKey.ToHex();
            if (obj.Address!= null && obj.Address != "") Address = obj.Address;

            if (obj.EncryptedSecKey != null) EncryptedSecKey = Printable.Hexify(obj.EncryptedSecKey);
            if (obj.PubKey.bytes != null) PubKey = obj.PubKey.ToHex();

            LastSeenHeight = obj.LastSeenHeight;
            Synced = obj.Synced;
            Syncer = obj.Syncer;

            if (encrypted)
            {
                Balance = 0;
            }
            else
            {
                Balance = obj.Balance;
            }

            if (obj.UTXOs != null)
            {
                UTXOs = new List<int>(obj.UTXOs.Count);

                for (int i = 0; i < obj.UTXOs.Count; i++)
                {
                    UTXOs.Add(obj.UTXOs[i].OwnedIndex);
                }
            }

            if (obj.TxHistory != null)
            {
                TxHistory = new List<int>(obj.TxHistory.Count);

                for (int i = 0; i < obj.TxHistory.Count; i++)
                {
                    TxHistory.Add(obj.TxHistory[i].Index);
                }
            }
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(WalletsLegacy.WalletAddress))
            {
                return (T)ToObject();
            }
            else
            {
                throw new ReadableException(typeof(WalletAddress).FullName, typeof(T).FullName);
            }
        }

        public object ToObject()
        {
            WalletsLegacy.WalletAddress obj = new();

            obj.Name = Name;
            obj.Type = Type;
            obj.Deterministic = Deterministic;

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
            obj.UTXOs = new List<WalletsLegacy.UTXO>(UTXOs.Count);

            if (UTXOs != null)
            {
                WalletDB db = WalletDB.GetDB();

                for (int i = 0; i < UTXOs.Count; i++)
                {
                    obj.UTXOs.Add(db.GetWalletOutput(UTXOs[i]));
                }
            }

            if (TxHistory != null)
            {
                WalletDB db = WalletDB.GetDB();

                for (int i = 0; i < TxHistory.Count; i++)
                {
                    obj.TxHistory.Add(db.GetTxFromHistory(obj, TxHistory[i]));
                }
            }

            obj.Syncer = Syncer;
            obj.Synced = Synced;
            obj.LastSeenHeight = LastSeenHeight;

            obj.Encrypted = true;

            return obj;
        }

        public static WalletsLegacy.WalletAddress FromReadable(string json)
        {
            return (WalletsLegacy.WalletAddress)new WalletAddress(json).ToObject();
        }

        public static string ToReadable(WalletsLegacy.WalletAddress obj)
        {
            /* only exception we can expect from ToReadable based on object state; still stateless but ugly */
            if (!obj.Encrypted) throw new Exception("Cannot create readable from unencrypted wallet!");

            return new WalletAddress(obj).JSON();
        }
    }
}
