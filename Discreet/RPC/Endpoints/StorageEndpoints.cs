using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.RPC.Common;
using Discreet.Common;
using Discreet.Wallets;

namespace Discreet.RPC.Endpoints
{
    public static class StorageEndpoints
    {
        [RPCEndpoint("kv_get", APISet.STORAGE)]
        public static object KVGet(string key)
        {
            byte[] val;

            try
            {
                val = WalletDB.GetDB().KVGet(Encoding.UTF8.GetBytes(key));

                return Encoding.UTF8.GetString(val);
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to KVGet failed: {ex.Message}");

                return new RPCError($"could not get value with key {key}");
            }
        }

        [RPCEndpoint("kv_put", APISet.STORAGE)]
        public static object KVPut(string key, string value)
        {
            byte[] val;

            try
            {
                val = WalletDB.GetDB().KVPut(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value));

                return Encoding.UTF8.GetString(val);
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to KVPut failed: {ex.Message}");

                return new RPCError($"could not put value {value} with key {key}");
            }
        }

        [RPCEndpoint("kv_del", APISet.STORAGE)]
        public static object KVDel(string key)
        {
            byte[] val;

            try
            {
                val = WalletDB.GetDB().KVDel(Encoding.UTF8.GetBytes(key));

                return Encoding.UTF8.GetString(val);
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to KVDel failed: {ex.Message}");

                return new RPCError($"could not delete value with key {key}");
            }
        }

        [RPCEndpoint("kv_all", APISet.STORAGE)]
        public static object KVAll()
        {
            try
            {
                return WalletDB.GetDB().KVAll().ToDictionary(kvp => Encoding.UTF8.GetString(kvp.Key), kvp => Encoding.UTF8.GetString(kvp.Value));
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to KVAll failed: {ex.Message}");

                return new RPCError($"could not complete request");
            }
        }
    }
}
