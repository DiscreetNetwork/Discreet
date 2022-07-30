using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Cipher
{
    public class KeyComparer : IComparer<Key>
    {
        public int Compare(Key x, Key y)
        {
            return x.CompareTo(y);
        }
    }
}