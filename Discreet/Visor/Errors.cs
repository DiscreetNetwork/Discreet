using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Visor
{
    public class DecodeException: Exception
    {
        public DecodeException(string msg) : base(msg) { }

        public DecodeException(string entryPoint, string type) : base(entryPoint + ": error while decoding " + type) { }

        public DecodeException(string entryPoint, string type, string msg) : base(entryPoint + ": error while decoding " + type + ": " + msg) { }
    }

    public class DatabaseException: Exception
    {
        public DatabaseException(string msg) : base(msg) { }

        public DatabaseException(string entryPoint, string msg) : base(entryPoint + ": error while calling database function " + msg) { }
    }
}
