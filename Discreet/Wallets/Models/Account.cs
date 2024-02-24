using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discreet.Cipher;

namespace Discreet.Wallets.Models
{
    public class Account
    {
        public string Address { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public HashSet<UTXO> UTXOs { get; set; }
        [JsonIgnore]
        public SortedSet<UTXO> SortedUTXOs { get; set; }
        [JsonIgnore]
        public HashSet<HistoryTx> TxHistory { get; set; }
        [JsonIgnore]
        public HashSet<UTXO> SelectedUTXOs { get; set; }

        public Key? PubKey { get; set; }

        public Key? PubSpendKey { get; set; }
        public Key? PubViewKey { get; set; }

        public byte Type { get; set; }
        public bool Deterministic { get; set; }
        
        [JsonIgnore]
        public byte[] EncryptedSecKeyMaterial { get; set; }


        public SQLiteWallet Wallet;

        public ulong Balance = 0;

        public Key SecKey;

        public Key SecSpendKey;
        public Key SecViewKey;

        public bool Encrypted = false;
        public bool Syncing = false;
    }
}
