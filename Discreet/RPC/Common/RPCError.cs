using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.RPC.Common
{
    /**
     * Used by endpoints to return an error received during executing the request.
     */
    class RPCError
    {
        [JsonPropertyName("code")]
        public int ErrID { get; set; }
        [JsonPropertyName("message")]
        public string ErrMsg { get; set; }
        [JsonPropertyName("data")]
        public object Result { get; set; }

        public RPCError() { }

        public RPCError(int id, string msg, object res)
        {
            if (id > -32000)
            {
                id = -32000;
            }

            ErrID = id;
            ErrMsg = msg;
            Result = res;
        }

        public RPCError(int id, string msg)
        {
            if (id > -32000)
            {
                id = -32000;
            }

            ErrID = id;
            ErrMsg = msg;
            Result = null;
        }

        public RPCError(string msg)
        {
            ErrID = -32000; 
            ErrMsg = msg;
            Result = null;
        }
    }
}
