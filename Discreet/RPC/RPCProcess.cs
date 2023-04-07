using Discreet.Cipher;
using Discreet.Coin;
using Discreet.RPC.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Discreet.Common.Converters;
using System.Linq;
using System.Threading.Tasks;
using Discreet.Coin.Converters;

namespace Discreet.RPC
{
    public class RPCProcess
    {
        public static JsonSerializerOptions defaultOptions;

        public class RPCRequest
        {
            [JsonPropertyName("jsonrpc")]
            public string Jsonrpc { get; set; }
            [JsonPropertyName("method")]
            public string Method { get; set; }
            [JsonPropertyName("params")]
            public object[] Params { get; set; }
            [JsonPropertyName("id")]
            public string Id { get; set; }
        }

        public class RPCResponse
        {
            [JsonPropertyName("jsonrpc")]
            public string Jsonrpc { get { return "2.0"; } }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("result")]
            public object Result { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("error")]
            public object Error { get; set; }
            [JsonPropertyName("id")]
            public object Id { get; set; }
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
                    new StealthAddressConverter(),
                    new TAddressConverter(),
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                    new StringConverter(),

                    // Coin.Converters
                    new BlockConverter(),
                    new BlockHeaderConverter(),
                    new BulletproofConverter(),
                    new BulletproofPlusConverter(),
                    new Coin.Converters.SignatureConverter(),
                    new TransactionConverter(),
                    new Coin.Converters.Transparent.TXInputConverter(),
                    new Coin.Converters.Transparent.TXOutputConverter(),
                    new TriptychConverter(),
                    new Coin.Converters.TXInputConverter(),
                    new TXOutputConverter(),

                }).ForEach(x => defaultOptions.Converters.Add(x));
            }
        }

        private static bool IsValidJson(string req)
        {
            if (req is null) return false;

            try
            {
                JsonDocument.Parse(req);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }


        public async Task<object> ProcessRemoteCall(RPCServer server, string rpcJsonRequest, bool isAvailable = true)
        {
            if (!IsValidJson(rpcJsonRequest))
            {
                return CreateResponseJSON(server, new RPCResponse { Id = null, Error = new RPCError { ErrID = -32700, ErrMsg = "Parse Error" } });
            }

            RPCRequest request = null;

            try
            {
                request = JsonSerializer.Deserialize<RPCRequest>(rpcJsonRequest, defaultOptions);
                if (request is null || request.Jsonrpc != "2.0") throw new JsonException();
            }
            catch (JsonException)
            {
                return CreateResponseJSON(server, new RPCResponse { Id = request?.Id, Error = new RPCError { ErrID = -32600, ErrMsg = "Invalid Request" } });
            }

            try
            {
                //var req = JsonDocument.Parse(Encoding.UTF8.GetBytes(rpcJsonRequest));
                object result = await ExecuteInternal(request.Method, isAvailable, server.Set, request.Params);
                RPCResponse response = CreateResponse(request, result);

                return CreateResponseJSON(server, response);
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Discreet.RPC.ProcessRemoteCall: parsing RPC request failed: {ex.Message}", ex);
                Daemon.Logger.Debug($"Discreet.RPC.ProcessRemoteCall: malformed RPC call received: {rpcJsonRequest}");
                return new RPCResponse { Error = new RPCError(-32001, "Server Error", "An undefined error was encountered."), Id = request?.Id };
            }

            return null;
        }

        //https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Function.cs
        private static bool IsFunc(Delegate del)
        {
            return del.GetType().GetGenericTypeDefinition() == typeof(Func<>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,,,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,,,,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,,,,,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,,,,,,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,,,,,,,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,,,,,,,,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,,,,,,,,,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,,,,,,,,,,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,,,,,,,,,,,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,,,,,,,,,,,,,>)
                || del.GetType().GetGenericTypeDefinition() == typeof(Func<,,,,,,,,,,,,,,,,>);
        }

        private static bool IsRPCCallAsync(Delegate del)
        {
            return del.GetType().IsGenericType && IsFunc(del)
                && del.GetType().GenericTypeArguments != null && del.GetType().GenericTypeArguments.Length > 0
                && del.GetType().GenericTypeArguments[del.GetType().GenericTypeArguments.Length - 1].IsGenericType
                && del.GetType().GenericTypeArguments[del.GetType().GenericTypeArguments.Length - 1].GetGenericTypeDefinition() == typeof(Task<>);
        }

        private async Task<object> ExecuteInternal(string endpoint, bool isAvailable, APISet enabledSets, params object[] args)
        {
            // this is a nonstandard endpoint used for checking daemon liveliness. 
            if (endpoint == "daemon_live")
            {
                return isAvailable;
            }

            if (!isAvailable)
            {
                return "not available";
            }

            Delegate _endpoint;
            APISet _set;

            try
            {
                _endpoint = RPCEndpointResolver.GetEndpoint(endpoint);
                _set = RPCEndpointResolver.GetSet(endpoint);
            }
            catch
            {
                Daemon.Logger.Error($"RPCProcess.ExecuteInternal: could not find endpoint with name \"{endpoint}\"");
                return new RPCError { ErrID = -32601, ErrMsg = "Method not found", Result = $"Host does not implement an endpoint with name \"{endpoint}\"" };
            }

            if (!enabledSets.HasFlag(_set))
            {
                return new RPCError
                {
                    ErrID = -32099,
                    ErrMsg = "Server Error",
                    Result = $"RPC server does not have the sets for this endpoint enabled (sets needed: {_set.Descriptor()}; enabled sets: {enabledSets.Descriptor()})"
                };
            }

            object[] _data = new object[args.Length];

            try
            {
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
            }
            catch
            {
                return new RPCError { ErrID = -32602, ErrMsg = "Invalid Params", Result = "Host could not properly parse given parameters" };
            }
            
            try
            {
                if (IsRPCCallAsync(_endpoint))
                {
                    // returns AsyncTaskMethodBuilder.AsyncStateMachineBox, inherits Task<TResult>
                    var del = _endpoint.DynamicInvoke(_data);
                    var res = await (del as Task<object>);
                    //Daemon.Logger.Critical($"{res.GetType().FullName}\n{endpoint}");
                    return res;
                }
                else
                {
                    object result = _endpoint.DynamicInvoke(_data);
                    return result;
                }
            }
            catch
            {
                return new RPCError { ErrID = -32603, ErrMsg = "Internal Error", Result = "An exception was thrown while trying to execute the given method with the given parameters." };
            }
        }

        public RPCResponse CreateResponse(RPCRequest request, object result)
        {
            /* Per: https://www.jsonrpc.org/specification
            * This member is REQUIRED.
            * It MUST be the same as the value of the id member in the Request Object.
            * If there was an error in detecting the id in the Request object (e.g. Parse error/Invalid Request), it MUST be Null
            */

            RPCResponse response = new();
            if(request.Id == String.Empty)
            {
                response.Id = null;
            }
            else
            {
                response.Id = request.Id;
            }

            if (result is RPCError rpcError)
            {
                response.Error = rpcError;
                return response;
            }

            response.Result = result;
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
