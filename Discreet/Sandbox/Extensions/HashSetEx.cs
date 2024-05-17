using Discreet.Cipher;
using Discreet.Coin.Models;
using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Sandbox.Extensions
{
    public static class HashSetEx
    {
        public static bool Contains<T>(this HashSet<T> h, TXInput input) where T : SandboxUtxo
        {
            SandboxUtxo toTest = new SandboxUtxo
            {
                Type = 0,
                LinkingTag = input.KeyImage,
            };

            return h.Contains(toTest);
        }

        public static bool TryGetValue<T>(this HashSet<T> h, TXInput input, out T value) where T : SandboxUtxo
        {
            SandboxUtxo toTest = new SandboxUtxo
            {
                Type = 0,
                LinkingTag = input.KeyImage,
            };

            return h.TryGetValue((T)toTest, out value);
        }

        public static bool Contains<T>(this HashSet<T> h, TTXInput input) where T : SandboxUtxo
        {
            SandboxUtxo toTest = new SandboxUtxo
            {
                Type = 1,
                TxSrc = input.TxSrc,
                OutputIndex = input.Offset
            };

            return h.Contains(toTest);
        }

        public static bool TryGetValue<T>(this HashSet<T> h, TTXInput input, out T value) where T : SandboxUtxo
        {
            SandboxUtxo toTest = new SandboxUtxo
            {
                Type = 1,
                TxSrc = input.TxSrc,
                OutputIndex = input.Offset
            };

            return h.TryGetValue((T)toTest, out value);
        }
    }
}
