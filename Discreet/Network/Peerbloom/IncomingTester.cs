using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    /// <summary>
    /// Used to check incoming connection's availability, primarily to prevent junk or private peers from accidentally being added to New.
    /// </summary>
    public class IncomingTester
    {
        private Network _network;
        private Peerlist _peerlist;

        private ConcurrentQueue<IPEndPoint> _tests = new();

        private ConcurrentDictionary<IPEndPoint, Connection> _connections = new();

        private const int _maxConnections = 3;

        public void Enqueue(IPEndPoint endpoint)
        {
            _tests.Enqueue(endpoint);
        }

        public IncomingTester(Network network, Peerlist peerlist)
        {
            _network = network;
            _peerlist = peerlist;
        }

        private async Task Feel(Connection conn, CancellationToken token = default)
        {
            var success = await conn.ConnectTest();

            if (success && conn.Port < 49152)
            {
                _peerlist.AddNew(new IPEndPoint(conn.Receiver.Address, conn.Port), new IPEndPoint(_network.ReflectedAddress, Daemon.DaemonConfig.GetConfig().Port.Value), 60L * 60L * 10_000_000L);
            }

            _connections.Remove(conn.Receiver, out _);
        }

        public async Task Start(CancellationToken token = default)
        {
            while (!token.IsCancellationRequested)
            {
                while (!_tests.IsEmpty && _connections.Count < _maxConnections && !token.IsCancellationRequested)
                {
                    bool success = _tests.TryDequeue(out var testEndpoint);

                    if (!success)
                    {
                        break;
                    }

                    var conn = new Connection(testEndpoint, _network, _network.LocalNode);
                    _connections[testEndpoint] = conn;
                    _ = Task.Run(() => Feel(conn, token)).ConfigureAwait(false);
                }

                await Task.Delay(2000, token);
            }
        }
    }
}
