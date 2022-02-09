using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.RPC.Common
{
    /**
     * Used by endpoints to return an error received during executing the request.
     */
    class RPCError
    {
        public int ErrID { get; set; }
        public string ErrMsg { get; set; }
        public object Result { get; set; }

        public RPCError() { }

        public RPCError(int id, string msg, object res)
        {
            ErrID = id;
            ErrMsg = msg;
            Result = res;
        }

        public RPCError(int id, string msg)
        {
            ErrID = id;
            ErrMsg = msg;
            Result = null;
        }

        public RPCError(string msg)
        {
            ErrID = -1; 
            ErrMsg = msg;
            Result = null;
        }
    }
}
