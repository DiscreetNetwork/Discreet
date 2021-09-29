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
            RPCEndpoints endpoints = new RPCEndpoints();
            RPCRequest request = JsonSerializer.Deserialize<RPCRequest>(rpcJsonRequest);

            Type testType = typeof(RPCEndpoints);
            ConstructorInfo ctor = testType.GetConstructor(System.Type.EmptyTypes);

            if (ctor != null)
            {
                object instance = ctor.Invoke(null);
                MethodInfo methodInfo;
                switch (request.method)
                {
                    case "test_getStealthAddress":
                        Console.WriteLine($"[{DateTime.Now} RPC]: test_getStealthAddress executed");
                        methodInfo = testType.GetMethod("test_getStealthAddress");
                        break;

                    case "test_getMnemonic":
                        methodInfo = testType.GetMethod("test_getMnemonic");
                        break;

                    default:
                        throw new Exception("Invalid RPC call");
                }

                object result = methodInfo.Invoke(instance, null);

                RPCResponse response = CreateResponse(request, result);

                return CreateResponseJSON(response);
            }

            throw new Exception("Unable to resolve endpoint");
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
