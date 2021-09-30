using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Coin
{
    public static class Config
    {
        public static uint OutputVersion = 1;
        public static uint TransactionVersion = 1;

        /* DEBUG must be set to false outside of tests. */
        public static bool DEBUG = true;
    }
}
