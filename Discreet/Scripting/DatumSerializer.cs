using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Scripting
{
    public class DatumSerializer
    {
        private const int TagNull = 0;
        private const int TagInt = 1;
        private const int TagString = 2;
        private const int TagTuple = 3;
        private const int TagArray = 4;
        private const int TagMap = 5;
        private const int TagStruct = 6;

        public DatumSerializer(byte[] deserialize, byte[] memory, byte[] schema)
        {

        }
    }
}
