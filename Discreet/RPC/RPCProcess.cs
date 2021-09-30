using Discreet.Cipher;
using Discreet.Coin;
using Discreet.RPC.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using static Discreet.RPC.Common.Utilities;

namespace Discreet.RPC
{
    public class RPCProcess
    {

        public class RPCRequest
        {
            public string jsonrpc { get { return "2.0"; } }
            public string method { get; set; }
            public object[] @params { get; set; }
            public string id { get; set; }
        }

        public class RPCResponse
        {
            public string jsonrpc { get { return "2.0"; } }
            public object result { get; set; }
            public object id { get; set; }
        }

        enum JSONRPCType
        {
            Request = 1,
            Notification = 2 // Has no id field set.
        }


        public object ProcessRemoteCall(string rpcJsonRequest)
        {
            try
            {
             var serializeOptions = new JsonSerializerOptions();
            serializeOptions.Converters.Add(new StringConverter());
            RPCRequest request = JsonSerializer.Deserialize<RPCRequest>(rpcJsonRequest, serializeOptions);
            object result = ExecuteInternal(request.method, request.@params);
            RPCResponse response = CreateResponse(request, result);

            return CreateResponseJSON(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Discreet.RPC: parsing RPC request failed {ex.Message}");
            }

            return null;
        }

        private object ExecuteInternal(string endpoint, params object[] args)
        {
            object result = RPCEndpointResolver.GetEndpoint(endpoint).DynamicInvoke(args);
            return result;
        }

        public RPCResponse CreateResponse(RPCRequest request, object result)
        {

            /* Per: 
            * This member is REQUIRED.
            * It MUST be the same as the value of the id member in the Request Object.
            * If there was an error in detecting the id in the Request object (e.g. Parse error/Invalid Request), it MUST be Null
            */
            RPCResponse response = new RPCResponse();
            if(request.id == "")
            {
                request.id = null;
            } else
            {
                response.id = request.id;
            }

            
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
