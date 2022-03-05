using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.RPC.Common;
using Discreet.Coin;

namespace Discreet.RPC.Endpoints
{
    public static class ReadEndpoints
    {
        public class GetBlockCountRV
        {
            public long Height { get; set; }
            public string Status { get; set; }
            public bool Untrusted { get; set; }
            public bool Synced { get; set; }

            public GetBlockCountRV() { }
        }

        [RPCEndpoint("get_block_count", APISet.READ)]
        public static GetBlockCountRV GetBlockCount()
        {
            try
            {
                var _handler = Network.Handler.GetHandler();

                GetBlockCountRV _rv = new GetBlockCountRV
                {
                    Height = DB.DisDB.GetDB().GetChainHeight(),
                    Synced = _handler.State != Network.PeerState.Normal,
                    Untrusted = false
                };

                if (_handler.State != Network.PeerState.Normal && _handler.State != Network.PeerState.Startup)
                {
                    var _versions = Network.MessageCache.GetMessageCache().Versions.Values;

                    foreach (var _version in _versions)
                    {
                        if (_version.Height > _rv.Height)
                        {
                            _rv.Untrusted = true;
                            _rv.Height = _version.Height;
                        }
                    }
                }

                _rv.Status = "OK";

                return _rv;
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetBlockCount failed: {ex}");

                return new GetBlockCountRV
                {
                    Height = 0,
                    Synced = false,
                    Untrusted = false,
                    Status = "An internal error was encountered"
                };
            }
        }

        [RPCEndpoint("get_block_hash_by_height", APISet.READ)]
        public static string GetBlockHashByHeight(long height)
        {
            try
            {
                return DB.DisDB.GetDB().GetBlockHeader(height).BlockHash.ToHex();
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetBlockHashByHeight failed: {ex}");

                return "";
            }
        }
    }
}
