using Discreet.Cipher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Discreet.Wallets.Models
{
    public class UTXO
    {
        [JsonIgnore]
        public int Id { get; set; }

        // the account id corresponding to the owner of the utxo
        [JsonIgnore]
        public string Address { get; set; }

        public byte Type { get; set; }
        public bool IsCoinbase { get; set; }

        public SHA256 TransactionSrc { get; set; }
        public ulong Amount { get; set; }
        public uint Index { get; set; }

        // stealth UTXO only
        public Key UXKey { get; set; }
        public Key UXSecKey;
        public Key Commitment { get; set; }
        public int? DecodeIndex { get; set; }
        public Key TransactionKey { get; set; }
        [JsonIgnore]
        public ulong DecodedAmount { get; set; } = 0;
        public Key LinkingTag { get; set; }

        [JsonIgnore]
        public bool Encrypted { get; set; }
        [JsonIgnore]
        public Account Account { get; set; }
    }
}
