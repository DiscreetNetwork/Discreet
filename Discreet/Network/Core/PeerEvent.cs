using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Discreet.Network.Core
{
    class PeerEvent
    {
        public IPEndPoint SenderGossipEndPoint;
        public DateTime ReceivedDateTime;

        public PeerState State { get; private set; }
        public IPAddress IP { get; private set; }
        public ushort GossipPort { get; private set; }
        public byte Generation { get; private set; }
        public byte Service { get; private set; }
        public ushort ServicePort { get; private set; }

        public IPEndPoint GossipEndPoint
        {
            get { return new IPEndPoint(IP, GossipPort); }
        }

        private PeerEvent()
        {
        }

        public PeerEvent(IPEndPoint senderGossipEndPoint, DateTime receivedDateTime, IPAddress ip, ushort gossipPort, PeerState state, byte generation)
        {
            SenderGossipEndPoint = senderGossipEndPoint;
            ReceivedDateTime = receivedDateTime;

            IP = ip;
            GossipPort = gossipPort;
            State = state;
            Generation = generation;
        }

        public PeerEvent(IPEndPoint senderGossipEndPoint, DateTime receivedDateTime, Peer peer)
        {
            SenderGossipEndPoint = senderGossipEndPoint;
            ReceivedDateTime = receivedDateTime;

            IP = peer.IP;
            GossipPort = peer.GossipPort;
            State = peer.State;
            Generation = peer.Generation;
            Service = peer.Service;
        }

        public static PeerEvent ReadFrom(IPEndPoint senderGossipEndPoint, DateTime receivedDateTime, Stream stream, bool isSender = false)
        {
            if (stream.Position >= stream.Length)
            {
                return null;
            }

            var memberEvent = new PeerEvent
            {
                SenderGossipEndPoint = senderGossipEndPoint,
                ReceivedDateTime = receivedDateTime,

                IP = isSender ? senderGossipEndPoint.Address : stream.ReadIPAddress(),
                GossipPort = isSender ? (ushort)senderGossipEndPoint.Port : stream.ReadPort(),
                State = isSender ? PeerState.Alive : stream.ReadMemberState(),
                Generation = (byte)stream.ReadByte(),
            };

            if (memberEvent.State == PeerState.Alive)
            {
                memberEvent.Service = (byte)stream.ReadByte();
                memberEvent.ServicePort = stream.ReadPort();
            }

            return memberEvent;
        }

        public override string ToString()
        {
            return string.Format("Sender:{0} Received:{1} IP:{2} GossipPort:{3} State:{4} Generation:{5} Service:{6} ServicePort:{7}",
            SenderGossipEndPoint,
            ReceivedDateTime,
            IP,
            GossipPort,
            State,
            Generation,
            Service,
            ServicePort);
        }

        public bool Equal(PeerEvent memberEvent)
        {
            return memberEvent != null &&
                    IP.Equals(memberEvent.IP) &&
                    GossipPort == memberEvent.GossipPort &&
                    State == memberEvent.State &&
                    Generation == memberEvent.Generation &&
                    Service == memberEvent.Service &&
                    ServicePort == memberEvent.ServicePort;
        }

        public bool NotEqual(PeerEvent memberEvent)
        {
            return !Equal(memberEvent);
        }

    }
}
