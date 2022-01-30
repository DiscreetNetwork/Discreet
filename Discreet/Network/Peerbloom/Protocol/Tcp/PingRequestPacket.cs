using Discreet.Network.Peerbloom.Protocol.Common;

namespace Discreet.Network.Peerbloom.Protocol.Tcp
{
    public class PingRequestPacket
    {
        public int Port { get; set; }

        public PingRequestPacket(int port)
        {
            Port = port;
        }

        /// <summary>
        /// Used to convert the POCO into a byte array that is ready to be sent across the network
        /// </summary>
        /// <returns></returns>
        public byte[] ToNetworkByteArray()
        {
            WritePacketBase packet = new WritePacketBase();

            packet.WriteString(this.GetType().Name);
            packet.WriteInt(Port);

            return packet.ToNetworkByteArray();
        }
    }
}
