using System;

namespace Discreet.RPC.Common
{

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Struct)]
    public class RPCEndpoint : Attribute
    {
        public string endpoint_name;
      

        public RPCEndpoint(string endpoint_name)
        {
            this.endpoint_name = endpoint_name;

        }
    }
}
