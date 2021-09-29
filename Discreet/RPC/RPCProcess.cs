using Discreet.Cipher;
using Discreet.Coin;
using Discreet.RPC.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Discreet.RPC
{
    public class RPCProcess
    {

        public class RPCRequest
        {
            public string jsonrpc { get { return "2.0"; } }
            public string method { get; set; }
            public object[] @params { get; set; }
            public int id { get; set; }
        }

        public class RPCResponse
        {
            public string jsonrpc { get { return "2.0"; } }
            public object result { get; set; }
            public int id { get; set; }
        }

        enum JSONRPCType
        {
            Request = 1,
            Notification = 2 // Has no id field set.
        }


        public object ProcessRemoteCall(string rpcJsonRequest)
        {
     
            RPCRequest request = JsonSerializer.Deserialize<RPCRequest>(rpcJsonRequest);
            object result = ExecuteInternal(request.method);
            RPCResponse response = CreateResponse(request, result);

            return CreateResponseJSON(response);

            throw new Exception("Unable to resolve endpoint");
        }

        private object ExecuteInternal(string endpoint, params object[] args)
        {
            object result = RPCEndpointResolver.GetEndpoint(endpoint).DynamicInvoke(args);
            return result;
        }

        public RPCResponse CreateResponse(RPCRequest request, object result)
        {
            RPCResponse response = new RPCResponse();
            response.id = request.id;
            response.result = result;
            return response;
        }
        public string CreateResponseJSON(RPCResponse response)
        {
            string request = JsonSerializer.Serialize<RPCResponse>(response);
            return request;
        }




    }
}
