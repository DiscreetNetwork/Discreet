using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Coin
{
    public class VerifyException: Exception
    {
        public VerifyException(string msg) : base("Discreet.Coin.Verify: " + msg) { }

        public VerifyException(string type, string msg) : base("Discreet.Coin." + type + "Verify: " + msg) { }
    }
}
