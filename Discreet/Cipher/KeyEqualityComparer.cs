using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Cipher
{
    public class KeyEqualityComparer : IEqualityComparer<Key>
    {
        public bool Equals(Key x, Key y)
        {
            return x.Equals(y);
        }

        public int GetHashCode([DisallowNull] Key obj)
        {
            return obj.GetHashCode();
        }
    }
}
