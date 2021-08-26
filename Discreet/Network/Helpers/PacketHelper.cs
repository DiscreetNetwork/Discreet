using System;
using System.Collections.Generic;
using System.Text;
using Discreet.Cipher;
using Discreet.Network.Packets;

namespace Discreet.Network.Helpers
{
   public class PacketHelper
    {

       public static byte[] Checksum(byte[] message) // TODO: accept Message from standard packet struct
        {
            // 4 first bytes of a SHA256 double-hashed message = checksum
            byte[] checksum = new byte[4];
            Array.Copy(SHA256.HashData(SHA256.HashData(message).Bytes).Bytes, checksum, 4);
            return checksum;
        } 

        public static WireMessageType MessageType(WireMessage packet)
        {
            // TODO: Iterate over all XOR options and return all packet flags (i.e. ack & encrypted), network optimization...
            if(packet.Type.HasFlag(WireMessageType.Ping))
            {
                return WireMessageType.Ping;
            }
            return WireMessageType.None;
        }

        public static byte[] SerializePacket(WireMessage packet)
        {
            throw new Exception("To be implemented.");
        }

        public static WireMessage DeserializePacket(byte[] packet)
        {
            throw new Exception("To be implemented.");
        }

    }
}
