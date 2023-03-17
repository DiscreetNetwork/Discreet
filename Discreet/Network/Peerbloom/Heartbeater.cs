using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public class Heartbeater
    {
        private Network _network;

        public Heartbeater(Network network)
        {
            _network = network;
        }

        public async Task Heartbeat(CancellationToken token)
        {
            while (!token.IsCancellationRequested && (Handler.GetHandler() == null || Handler.GetHandler().State != PeerState.Normal))
            {
                await Task.Delay(500, token);
            }

            while (!token.IsCancellationRequested)
            {
                var timer = DateTime.UtcNow.AddSeconds(Constants.PEERBLOOM_HEARTBEATER).Ticks;

                while (timer > DateTime.UtcNow.Ticks && !token.IsCancellationRequested)
                {
                    await Task.Delay(500);
                }

                if (token.IsCancellationRequested) return;

                Daemon.Logger.Debug($"Heartbeater: testing connections...");

                var pingIDBytes = Cipher.Randomness.Random(8);
                var pingID = Common.Serialization.GetUInt64(pingIDBytes, 0);

                _network.handler.PingID = pingID;

                Core.Packet p = new Core.Packet(Core.PacketType.NETPING, new Core.Packets.Peerbloom.NetPing { Data = pingIDBytes });

                if (_network.LocalNode.IsPublic)
                {
                    foreach (var conn in _network.InboundConnectedPeers.Values)
                    {
                        if (new DateTime(conn.LastValidReceive).AddSeconds(Constants.PEERBLOOM_HEARTBEATER_TIMEOUT).Ticks < DateTime.UtcNow.Ticks)
                        {
                            conn.WasPinged = true;
                            conn.PingStart = DateTime.UtcNow.Ticks;
                            _network.Send(conn, p);
                        }
                    }
                }

                foreach (var conn in _network.OutboundConnectedPeers.Values)
                {
                    if (new DateTime(conn.LastValidReceive).AddSeconds(Constants.PEERBLOOM_HEARTBEATER_TIMEOUT).Ticks < DateTime.UtcNow.Ticks)
                    {
                        conn.WasPinged = true;
                        conn.PingStart = DateTime.UtcNow.Ticks;
                        _network.Send(conn, p);
                    }
                }
            }
        }
    }
}
