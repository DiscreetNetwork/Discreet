using Discreet.Cipher;
using Discreet.Coin;
using Discreet.RPC.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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


        public object ProcessRemoteCall(RPCServer server, string rpcJsonRequest)
        {
            try
            {
                //var req = JsonDocument.Parse(Encoding.UTF8.GetBytes(rpcJsonRequest));
                RPCRequest request = JsonSerializer.Deserialize<RPCRequest>(rpcJsonRequest, defaultOptions);
                object result = ExecuteInternal(request.method, request.@params);
                RPCResponse response = CreateResponse(request, result);

                return CreateResponseJSON(server, response);
            }
            catch (Exception ex)
            {
                Daemon.Logger.Log($"Discreet.RPC.ProcessRemoteCall: parsing RPC request failed: {ex.Message}");
                Daemon.Logger.Debug($"Discreet.RPC.ProcessRemoteCall: malformed RPC call received {rpcJsonRequest}");
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
            //Daemon.Logger.Debug($"Calling endpoint \"{endpoint}\"");
            var _paramInfo = _endpoint.Method.GetParameters();

            object[] _data = new object[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                /*if (_paramInfo[i + 1].ParameterType == typeof(Readable.FullTransaction))
                {
                    // this simplifies an edge case for TXN endpoints
                    var _version = ((JsonElement)args[i]).EnumerateObject().Where(x => x.NameEquals("Version")).First().Value.GetByte();

                    _data[i] = _version switch
                    {
                        0 or 1 or 2 => JsonSerializer.Deserialize((JsonElement)args[i], typeof(Readable.Transaction)),
                    };
                }*/
                if (args[i] == null)
                {
                    _data[i] = null;
                }
                else
                {
                    _data[i] = JsonSerializer.Deserialize((JsonElement)args[i], _paramInfo[i + 1].ParameterType, defaultOptions);
                }
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
            if(request.id == String.Empty)
            {
                response.id = null;
            }
            else
            {
                response.id = request.id;
            }

            response.result = result;
            return response;
        }
        public string CreateResponseJSON(RPCServer server, RPCResponse _response)
        {
            try
            {
                string response = JsonSerializer.Serialize<RPCResponse>(_response, defaultOptions);
                
                if (server.Indented)
                {
                    response = Discreet.Common.Printable.Prettify(response, server.UseTabs, server.IndentSize);
                }

                return response;
            } 
            catch(Exception ex)
            {
                throw new JsonException($"Failed to serialize response: {ex}");
            }
        }
    }
}
