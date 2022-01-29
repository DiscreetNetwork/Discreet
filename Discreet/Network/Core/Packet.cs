using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Discreet.Network.Core
{
    public class Packet
    {
        public PacketHeader Header { get; set; }

        public IPacketBody Body { get; set; }

        public PacketType Type()
        {
            return Header.Command;
        }

        public void Populate(Stream s)
        {
            Header = new PacketHeader(s);

            byte[] bodyData = new byte[Header.Length];
            int num = s.Read(bodyData);

            if (num < Header.Length)
            {
                throw new Exception($"Discreet.Network.Core.Packet.Populate: expected {Header.Length} bytes in payload, but got {num}");
            }

            Body = DecodePacketBody(Header.Command, bodyData);
        }

        public static IPacketBody DecodePacketBody(PacketType t, byte[] bodyData)
        {
            switch (t)
            {
                case PacketType.GETVERSION:
                    return new Packets.GetVersionPacket();
                case PacketType.VERSION:
                    return new Packets.VersionPacket(bodyData, 0);
                case PacketType.INVENTORY:
                    return new Packets.InventoryPacket(bodyData, 0);
                case PacketType.GETBLOCKS:
                    return new Packets.GetBlocksPacket(bodyData, 0);
                case PacketType.BLOCKS:
                    return new Packets.BlocksPacket(bodyData, 0);
                case PacketType.GETTXS:
                    return new Packets.GetTransactionsPacket(bodyData, 0);
                case PacketType.TXS:
                    return new Packets.TransactionsPacket(bodyData, 0);
                case PacketType.NOTFOUND:
                    return new Packets.NotFoundPacket(bodyData, 0);
                case PacketType.REJECT:
                    return new Packets.RejectPacket(bodyData, 0);
                case PacketType.ALERT:
                    return new Packets.AlertPacket(bodyData, 0);
                case PacketType.SENDTX:
                    return new Packets.SendTransactionPacket(bodyData, 0);
                case PacketType.SENDBLOCK:
                    return new Packets.SendBlockPacket(bodyData, 0);
                case PacketType.SENDMSG:
                    return new Packets.SendMessagePacket(bodyData, 0);
                case PacketType.NONE:
                    throw new Exception("Discreet.Network.Core.Packet.DecodePacketBody: dummy packet received (PacketType.NONE)");
                default:
                    throw new Exception($"Discreet.Network.Core.Packet.DecodePacketBody: unknown packet type {t}");
            }
        }
    }
}
