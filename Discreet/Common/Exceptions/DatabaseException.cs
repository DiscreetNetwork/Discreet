using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Common.Exceptions
{
    public class DatabaseException : Exception
    {
        public DatabaseException(string msg) : base(msg) { }

        public DatabaseException(string entryPoint, string msg) : base(entryPoint + ": error while calling database function " + msg) { }
    }
}
