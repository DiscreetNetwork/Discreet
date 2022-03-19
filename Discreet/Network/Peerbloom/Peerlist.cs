using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public class Peerlist
    {
        public class Peer
        {
            public IPEndPoint Endpoint { get; private set; }
            public long LastSeen { get; private set; }

            public byte[] Serialize()
            {
                byte[] data = new byte[26];

                Core.Utils.SerializeEndpoint(Endpoint, data, 0);
                Coin.Serialization.CopyData(data, 18, LastSeen);

                return data;
            }

            public Peer() { }

            public Peer(byte[] data)
            {
                Endpoint = Core.Utils.DeserializeEndpoint(data, 0);
                LastSeen = Coin.Serialization.GetInt64(data, 18);
            }

            public Peer(IPEndPoint endpoint, long lastSeen)
            {
                Endpoint = endpoint;
                LastSeen = lastSeen;
            }

            public override int GetHashCode()
            {
                return Endpoint.GetHashCode();
            }
        }

        private List<Bucket> Tried;
        private List<Bucket> New;

        private List<Peer> Anchors;

        public Peerlist()
        {
            //Tried = DB.DisDB.GetDB().GetTried();
            //New = DB.DisDB.GetDB().GetNew();
        }

        public void AddPeer(IPEndPoint endpoint = null, long lastSeen = 0)
        {
            // WIP
            if (endpoint == null) return;

            /* sets last seen to 24 hours ago, if not set */
            if (lastSeen <= 0) lastSeen = DateTime.UtcNow.Ticks - (86400L * 10000L * 1000L);

            var peer = new Peer(endpoint, lastSeen);

            //_peers.Add(peer);
            //DB.DisDB.GetDB().AddPeer(peer);
        }
    }
}
