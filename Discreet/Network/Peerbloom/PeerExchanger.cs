using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public class PeerExchanger
    {
        private Network _network;

        public PeerExchanger(Network network)
        {
            _network = network;
        }

        public async Task Exchange(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var timer = DateTime.UtcNow.AddSeconds(Constants.PEER_EXCHANGER_TIMER).Ticks;

                while (timer > DateTime.UtcNow.Ticks && !token.IsCancellationRequested)
                {
                    await Task.Delay(500);
                }

                if (token.IsCancellationRequested) return;

                Daemon.Logger.Debug($"PeerExchanger: requesting peers...");

                Random r = new Random();

                if (_network.LocalNode.IsPublic)
                {
                    for (int i = 0; i < Constants.PEER_EXCHANGER_INBOUND; i++)
                    {
                        var peer = _network.InboundConnectedPeers.Values.ToList()[r.Next(0, _network.InboundConnectedPeers.Count)];

                        _network.Send(peer, new Core.Packet(Core.PacketType.REQUESTPEERS, new Core.Packets.Peerbloom.RequestPeers { Endpoint = _network.LocalNode.Endpoint, MaxPeers = Constants.PEERBLOOM_MAX_PEERS_PER_REQUESTPEERS }));
                    }
                }

                for (int i = 0; i < Constants.PEER_EXCHANGER_OUTBOUND; i++)
                {
                    var peer = _network.OutboundConnectedPeers.Values.ToList()[r.Next(0, _network.OutboundConnectedPeers.Count)];

                    _network.Send(peer, new Core.Packet(Core.PacketType.REQUESTPEERS, new Core.Packets.Peerbloom.RequestPeers { Endpoint = _network.LocalNode.Endpoint, MaxPeers = Constants.PEERBLOOM_MAX_PEERS_PER_REQUESTPEERS }));
                }
            }
        }
    }
}
