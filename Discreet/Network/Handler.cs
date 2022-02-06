using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Network.Core;

namespace Discreet.Network
{
    public delegate void OnConnectEvent();

    public class Handler
    {
        public PeerState State { get; private set; }

        public bool IsMasternode { get; private set; }

        private static Handler _handler;

        private static object handler_lock = new object();

        public static Handler GetHandler()
        {
            lock (handler_lock)
            {
                if (_handler == null) Initialize();

                return _handler;
            }
        }

        public static void Initialize()
        {
            lock (handler_lock)
            {
                if (_handler == null)
                {
                    _handler = new Handler();
                }
            }
        }

        public Handler() 
        {
            State = PeerState.Normal;

            IsMasternode = false;
        }

        /* handles incoming packets */
        public async Task Handle(Packet p)
        {
            //WIP

            /* REMOVE IN FUTURE; USED FOR TESTING */
            if (p.Header.Command == Core.PacketType.OLDMESSAGE)
            {
                var body = (Core.Packets.Peerbloom.OldMessage)p.Body;

                Peerbloom.Network.GetNetwork().OnSendMessageReceived(body.MessageID, body.Message);
            }
            else
            {
                Visor.Logger.Log($"Unknown/unimplemented packet type received: {p.Header.Command}");
                return;
            }
        }

        public void Handle(string s)
        {
            Visor.Logger.Log(s);
        }
    }
}
