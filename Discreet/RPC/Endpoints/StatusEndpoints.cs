using Discreet.RPC.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.RPC.Endpoints
{
    /// <summary>
    /// Contains status endpoints for the RPC API. Status endpoints are used to get information about the node in relation to the network.
    /// </summary>
    public static class StatusEndpoints
    {
        [RPCEndpoint("get_version", APISet.STATUS)]
        public static object GetVersion()
        {
            try
            {
                return Network.Handler.GetHandler().MakeVersionPacket();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetVersion failed: {ex.Message}");

                return new RPCError($"Could not get version data");
            }
        }
    }
}
