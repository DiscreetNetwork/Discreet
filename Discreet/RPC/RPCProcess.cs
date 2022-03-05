using Discreet.Cipher;
using Discreet.Coin;
using Discreet.RPC.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Discreet.RPC.Common.Utilities;
using Discreet.RPC.Converters;
using System.Linq;

namespace Discreet.RPC
{
    public class RPCProcess
    {
        public static JsonSerializerOptions defaultOptions;

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

        public RPCProcess()
        {
            if (defaultOptions == null)
            {
                defaultOptions = new JsonSerializerOptions();

                new List<JsonConverter>(new JsonConverter[]
                {
                    new BytesConverter(),
                    new IAddressConverter(),
                    new IPEndPointConverter(),
                    new KeccakConverter(),
                    new KeyConverter(),
                    new RIPEMD160Converter(),
                    new SHA256Converter(),
                    new SHA512Converter(),
                    new SignatureConverter(),
                    new StealthAddressConverter(),
                    new TAddressConverter(),
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                    new StringConverter(),
                }).ForEach(x => defaultOptions.Converters.Add(x));
            }
        }


        public object ProcessRemoteCall(string rpcJsonRequest)
        {
            try
            {
                //var req = JsonDocument.Parse(Encoding.UTF8.GetBytes(rpcJsonRequest));
                RPCRequest request = JsonSerializer.Deserialize<RPCRequest>(rpcJsonRequest, defaultOptions);
                object result = ExecuteInternal(request.method, request.@params);
                RPCResponse response = CreateResponse(request, result);

                return CreateResponseJSON(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Discreet.RPC: parsing RPC request failed: {ex.Message}");
            }

            return null;
        }

        private object ExecuteInternal(string endpoint, params object[] args)
        {
            var _endpoint = RPCEndpointResolver.GetEndpoint(endpoint);

            /**
             * A note on MethodInfo for delegates.
             * The first parameter in the ParameterInfo for the delegate's method base is always the Target.
             * This Target includes captured variables and, if the delegate comes from a specific class instance, the 'this' of that class.
             * It is the "environment" that is enclosed by the delegate, thus a closure.
             * I have tested and it seems that both closed and open delegates always have a Target as the first parameter.
             * Thus we skip this, as can be seen where ConvertType gets _paramInfo[i + 1].ParameterType.
             */
            var _paramInfo = _endpoint.Method.GetParameters();

            object[] _data = new object[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                _data[i] = ConvertType(_paramInfo[i + 1].ParameterType, (JsonElement)args[i]);
            }

            object result = _endpoint.DynamicInvoke(_data);
            return result;
        }

        public RPCResponse CreateResponse(RPCRequest request, object result)
        {

            /* Per: https://www.jsonrpc.org/specification
            * This member is REQUIRED.
            * It MUST be the same as the value of the id member in the Request Object.
            * If there was an error in detecting the id in the Request object (e.g. Parse error/Invalid Request), it MUST be Null
            */
            RPCResponse response = new();
            if(request.id == "")
            {
                response.id = null;
            } else
            {
                response.id = request.id;
            }

            response.result = result;
            return response;
        }
        public string CreateResponseJSON(RPCResponse response)
        {
            try
            {
                string request = JsonSerializer.Serialize<RPCResponse>(response);
                return request;
            } catch(Exception ex)
            {
                throw new JsonException($"Failed to serialize response: {ex}");
            }
        }
    }
}
