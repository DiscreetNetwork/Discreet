using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.RPC;
using Discreet.RPC.Common;
using Discreet.Wallets;
using Discreet.Network;
using Discreet.Coin;

namespace Discreet.RPC.Endpoints
{
    public static class DebugEndpoints
    {
        [RPCEndpoint("dbg_faucet_stealth_payout")]
        public static object DbgFaucetStealthPayout(string address, ulong amount)
        {
            try
            {
                var _visor = Handler.GetHandler().visor;

                if (_visor.IsMasternode)
                {
                    var addr = _visor.wallets.First().Addresses[0];

                    var tx = addr.CreateTransaction(new StealthAddress(address), amount).ToFull();

                    _ = Network.Peerbloom.Network.GetNetwork().Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDTX, new Network.Core.Packets.SendTransactionPacket { Tx = tx }));

                    return tx.Hash().ToHex();
                }
                else
                {
                    return new RPCError("you are not a masternode!");
                }
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to DbgFaucet failed: {ex.Message}");

                return new RPCError($"Could not fulfill faucet request");
            }
        }

        [RPCEndpoint("dbg_faucet_transparent_payout")]
        public static object DbgFaucetTransparentPayout(string address, ulong amount)
        {
            try
            {
                var _visor = Handler.GetHandler().visor;

                if (_visor.IsMasternode)
                {
                    var addr = _visor.wallets.First().Addresses[0];

                    var tx = addr.CreateTransaction(new IAddress[] { new TAddress(address) }, new ulong[] { amount }).ToFull();

                    _ = Network.Peerbloom.Network.GetNetwork().Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDTX, new Network.Core.Packets.SendTransactionPacket { Tx = tx }));

                    return tx.Hash().ToHex();
                }
                else
                {
                    return new RPCError("you are not a masternode!");
                }
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to DbgFaucet failed: {ex.Message}");

                return new RPCError($"Could not fulfill faucet request");
            }
        }
    }
}
