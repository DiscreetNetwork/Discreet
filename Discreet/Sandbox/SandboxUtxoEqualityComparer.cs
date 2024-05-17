using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Sandbox
{
    public class SandboxUtxoEqualityComparer : IEqualityComparer<SandboxUtxo>
    {
        public bool Equals(SandboxUtxo? x, SandboxUtxo? y)
        {
            if (x == null && y == null) return true;
            if (x == y) return true;
            if (x == null || y == null) return false;

            if (x.Type != y.Type) return false;
            if (x.Type == 0)
            {
                if (x.LinkingTag == y.LinkingTag) return true;
            }
            else
            {
                if (x.TxSrc == y.TxSrc && x.OutputIndex == y.OutputIndex) return true;
            }

            return false;
        }

        public int GetHashCode([DisallowNull] SandboxUtxo obj)
        {
            if (obj.Type == 0)
            {
                return Common.Serialization.GetInt32(obj.LinkingTag.bytes ?? new byte[4], 0);
            }
            else
            {
                return Common.Serialization.GetInt32(obj.TxSrc.Bytes, 0);
            }
        }
    }
}
