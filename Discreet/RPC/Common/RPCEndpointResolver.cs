using Discreet.Cipher;
using Discreet.Coin;
using Discreet.RPC.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;

namespace Discreet.RPC
{
   public class RPCEndpointResolver
    {
        private static Dictionary<string, Delegate> endpoints = new Dictionary<string, Delegate> { };


        public static void ReflectEndpoints()
        {
             var methods = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.IsClass)
            .SelectMany(x => x.GetMethods())
            .Where(x => x.GetCustomAttributes(typeof(RPCEndpoint), false).FirstOrDefault() != null);


            foreach (MethodInfo method in methods)
            {
                // Get delegate for method/action
                Delegate methodDelegate = CreateMethod(method);
                RPCEndpoint RPCMethodName = (RPCEndpoint)method.GetCustomAttributes(typeof(RPCEndpoint), true)[0];
                endpoints.Add(RPCMethodName.endpoint_name, methodDelegate);
            }

            Visor.Logger.Log($"{endpoints.Count} RPC endpoints loaded successfully.");
        }

        public static Delegate GetEndpoint(string endpoint)
        {
            return endpoints[endpoint];
        }

        public static Delegate CreateMethod(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException("Discreet.RPC: The supplied method was null.");
            }

            if (!method.IsStatic)
            {
                throw new ArgumentException("Discreet.RPC: The supplied method must be static (stateless).", nameof(method));
            }

            if (method.IsGenericMethod)
            {
                throw new ArgumentException("Discreet.RPC: The supplied method must not be generic.", nameof(method));
            }

            var parameters = method.GetParameters()
                                   .Select(p => Expression.Parameter(p.ParameterType, p.Name))
                                   .ToArray();

            MethodCallExpression call = Expression.Call(null, method, parameters);

            return Expression.Lambda(call, parameters).Compile();
        }
    }
}
