using Discreet.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.DB
{
    public class AlreadyPresentException : VerifyException
    {
        public AlreadyPresentException(string msg) : base("Discreet.Coin.Verify: " + msg) { }

        public AlreadyPresentException(string type, string msg) : base("Discreet.Coin." + type + ".Verify: " + msg) { }

        public AlreadyPresentException(string type, string vertype, string msg) : base("Discreet.Coin." + type + "Verify" + vertype + ": " + msg) { }
    }
}
