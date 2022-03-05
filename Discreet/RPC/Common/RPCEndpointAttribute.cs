using System;

namespace Discreet.RPC.Common
{

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Struct)]
    public class RPCEndpoint : Attribute
    {
        public string endpoint_name;

        public APISet set;
      

        public RPCEndpoint(string endpoint_name, APISet set)
        {
            this.endpoint_name = endpoint_name;
            this.set = set;
        }

        public RPCEndpoint(string endpoint_name)
        {
            this.endpoint_name = endpoint_name;
            this.set = APISet.DEFAULT;
        }
    }
}
