using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discreet.Cipher;

namespace Discreet.Wallets.Models
{
    public class HistoryTxOutput
    {
        public ulong Amount { get; set; }
        public string Address { get; set; }
    }

    public class HistoryTx
    {
        [JsonIgnore]
        public int Id { get; set; }
        [JsonIgnore]
        public byte[] EncryptedRawData { get; set; }
        [JsonIgnore] 
        public string Address { get; set; }

        /* populated fields */
        public SHA256 TxID { get; set; }
        public long Timestamp { get; set; }

        public ulong SentAmount { get
            {
                long total = 0;
                if (Inputs != null)
                {
                    total -= Inputs.Where(x => x.Address == Address).Select(x => (long)x.Amount).Aggregate(0L, (x, y) => x + y);
                }

                if (Outputs != null)
                {
                    total += Outputs.Where(x => x.Address == Address).Select(x => (long)x.Amount).Aggregate(0L, (x, y) => x + y);
                }

                return (total < 0) ? (ulong)(-total) : 0;
            } }

        public ulong ReceivedAmount
        {
            get
            {
                long total = 0;
                if (Inputs != null)
                {
                    total -= Inputs.Where(x => x.Address == Address).Select(x => (long)x.Amount).Aggregate(0L, (x, y) => x + y);
                }

                if (Outputs != null)
                {
                    total += Outputs.Where(x => x.Address == Address).Select(x => (long)x.Amount).Aggregate(0L, (x, y) => x + y);
                }

                return (total > 0) ? (ulong)total : 0;
            }
        }

        public List<HistoryTxOutput> Inputs { get; set; }
        public List<HistoryTxOutput> Outputs { get; set; }

        [JsonIgnore]
        public Account Account { get; set; }

        public HistoryTx() { }

        public HistoryTx(Account account, SHA256 txid, ulong[] inputs, string[] senders, ulong[] outputs, string[] receivers, long timestamp = 0)
        {
            Account = account;
            TxID = txid;
            Timestamp = timestamp;
            Inputs = senders.Zip(inputs).Select(x => new HistoryTxOutput { Address = x.First, Amount = x.Second }).ToList();
            Outputs = receivers.Zip(outputs).Select(x => new HistoryTxOutput { Address = x.First, Amount = x.Second }).ToList();
            Timestamp = timestamp;
            Address = account.Address;
        }
    }
}
