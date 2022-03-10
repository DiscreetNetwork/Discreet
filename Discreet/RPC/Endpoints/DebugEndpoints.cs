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
        [RPCEndpoint("dbg_faucet_stealth")]
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
                Daemon.Logger.Error($"RPC call to DbgFaucet failed: {ex.Message}");

                return new RPCError($"Could not fulfill faucet request");
            }
        }

        public class DbgFaucetTransparentRV
        {
            public Readable.FullTransaction Tx { get; set; }
            public string Txid { get; set; }
            public string Verify { get; set; }
        }

        [RPCEndpoint("dbg_faucet_transparent")]
        public static object DbgFaucetTransparentPayout(string address, ulong amount)
        {
            try
            {
                var _visor = Handler.GetHandler().visor;

                if (_visor.IsMasternode)
                {
                    var addr = _visor.wallets.First().Addresses[0];

                    var tx = addr.CreateTransaction(new IAddress[] { new TAddress(address) }, new ulong[] { amount }).ToFull();

                    var verify = tx.Verify();
                    string _verify = "";

                    if (verify == null)
                    {
                        _verify = "all good!";
                    }
                    else
                    {
                        _verify = verify.Message;
                    }

                    _ = Network.Peerbloom.Network.GetNetwork().Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDTX, new Network.Core.Packets.SendTransactionPacket { Tx = tx }));

                    return new DbgFaucetTransparentRV
                    {
                        Tx = (Readable.FullTransaction)tx.ToReadable(),
                        Txid = tx.Hash().ToHex(),
                        Verify = _verify
                    };
                }
                else
                {
                    return new RPCError("you are not a masternode!");
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to DbgFaucet failed: {ex}");

                return new RPCError($"Could not fulfill faucet request");
            }
        }
    }
}
