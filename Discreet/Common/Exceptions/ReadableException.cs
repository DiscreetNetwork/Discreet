using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Common.Exceptions
{
    public class ReadableException: Exception
    {
        public ReadableException(string type, string type2) : base(type + ": Did not expect to convert to object of type " + type2) { }
    }
}
