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
            public int result { get; set; }
            public int id { get; set; }
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
                        Console.WriteLine("RPC: invoking test_getStealthAddress");
                        methodInfo = testType.GetMethod("test_getStealthAddress");
                        break;

                    case "test_getMnemonic":
                        methodInfo = testType.GetMethod("test_getMnemonic");
                        break;

                    default:
                        throw new Exception("Invalid RPC call");
                }

                object result = methodInfo.Invoke(instance, null);

                return result;
            }

            throw new Exception("Unable to resolve endpoint");
        }

        public RPCResponse CreateResponse(string response)
        {
            return null;
        }




    }
}
