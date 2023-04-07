using Discreet.Coin.Models;
using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Coin.Comparers
{
    public class TTXInputEqualityComparer : IEqualityComparer<TTXInput>
    {
        public bool Equals(TTXInput x, TTXInput y) => x.Equals(y);

        public int GetHashCode([DisallowNull] TTXInput obj) => Cipher.SHA256.HashData(obj.Serialize()).GetHashCode();
    }
}
