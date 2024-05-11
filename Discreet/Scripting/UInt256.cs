using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Discreet.Scripting
{
    /// <summary>
    /// For now, unused.
    /// </summary>
    //[StructLayout(LayoutKind.Explicit)]
    //public readonly struct UInt256
    //{
    //    [FieldOffset(0)]
    //    public readonly ulong l0;

    //    [FieldOffset(8)]
    //    public readonly ulong l1;

    //    [FieldOffset(16)]
    //    public readonly ulong l2;

    //    [FieldOffset(24)]
    //    public readonly ulong l3;

    //    public UInt256(ulong _l0 = 0, ulong _l1 = 0, ulong _l2 = 0, ulong _l3 = 0)
    //    {
    //        if (Avx2.IsSupported)
    //        {
    //            Unsafe.SkipInit(out l0);
    //            Unsafe.SkipInit(out l1);
    //            Unsafe.SkipInit(out l2);
    //            Unsafe.SkipInit(out l3);

    //            Unsafe.As<ulong, Vector256<ulong>>(ref l0) = Vector256.Create(_l0, _l1, _l2, _l3);
    //        }
    //        else
    //        {
    //            l0 = _l0;
    //            l1 = _l1;
    //            l2 = _l2;
    //            l3 = _l3;
    //        }
    //    }

    //    public static UInt256 Negate(in UInt256 a)
    //    {
    //        ulong cs0 = 0 - a.l0;
    //        ulong cs1 = 0 - a.l1;
    //        ulong cs2 = 0 - a.l2;
    //        ulong cs3 = 0 - a.l3;
    //        if (a.l0 > 0)
    //        {
    //            cs3--;
    //        }

    //        return new UInt256(cs0, cs1, cs2, cs3);
    //    }

    //    public UInt256()
    //    {
    //        l0 = 0;
    //        l1 = 0;
    //        l2 = 0;
    //        l3 = 0;
    //    }

    //    public UInt256()

    //    public UInt256 Add(UInt256 v)
    //    {
    //        ulong _l0 = unchecked(v.l0 + l0);
    //        ulong carry = ((v.l0 ^ v.l1) >= 0)
    //    }

    //    public UInt256 Mul(UInt256 v)
    //    {
    //        Math.BigMul(0l, 0l, out l0);
    //    }
    //}
}
