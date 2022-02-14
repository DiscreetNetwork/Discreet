using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Coin
{
    public static class Config
    {
        public static uint TransactionVersion = 1;

        public enum TransactionVersions: byte
        {
            NULL = 0, /* coinbase and genesis */
            STANDARD = 1, /* triptych and bulletproof */
            BP_PLUS = 2, /* triptych and bulletproof+ */
            TRANSPARENT = 3, /* bitcoin-style */
            MIXED = 4, /* zcash-style */
        }

        /* DEBUG must be set to false outside of tests. */
        public static bool DEBUG = true;

        public static uint TRANSPARENT_MAX_NUM_INPUTS = 255;
        public static uint TRANSPARENT_MAX_NUM_OUTPUTS = 255;
    }
}
