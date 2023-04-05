using Discreet.Cipher;
using Discreet.Coin.Models;
using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets.Extensions
{
    public static class HashSetEx
    {
        public static bool Contains<T>(this HashSet<T> h, TXInput input) where T : UTXO
        {
            UTXO toTest = new UTXO
            {
                Type = 0,
                LinkingTag = input.KeyImage,
            };

            return h.Contains(toTest);
        }

        public static bool TryGetValue<T>(this HashSet<T> h, TXInput input, out T value) where T : UTXO
        {
            UTXO toTest = new UTXO
            {
                Type = 0,
                LinkingTag = input.KeyImage,
            };

            return h.TryGetValue((T)toTest, out value);
        }

        public static bool Contains<T>(this HashSet<T> h, TTXInput input) where T : UTXO
        {
            UTXO toTest = new UTXO
            {
                Type = 1,
                TransactionSrc = input.TxSrc,
                Index = input.Offset
            };

            return h.Contains(toTest);
        }

        public static bool TryGetValue<T>(this HashSet<T> h, TTXInput input, out T value) where T : UTXO
        {
            UTXO toTest = new UTXO
            {
                Type = 1,
                TransactionSrc = input.TxSrc,
                Index = input.Offset
            };

            return h.TryGetValue((T)toTest, out value);
        }

        public static bool Contains<T>(this HashSet<T> h, SHA256 txid) where T : HistoryTx
        {
            HistoryTx tx = new HistoryTx
            {
                TxID = txid
            };

            return h.Contains(tx);
        }
    }
}
