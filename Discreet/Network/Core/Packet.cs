using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace Discreet.Network.Core
{
    public class Packet
    {
        public PacketHeader Header { get; set; }

        public IPacketBody Body { get; set; }

        public Packet() { }

        public Packet(Stream s)
        {
            Populate(s);
        }

        public Packet(byte[] bytes)
        {
            Populate(bytes);
        }

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

            uint _checksum = Coin.Serialization.GetUInt32(SHA256.HashData(SHA256.HashData(bodyData)), 0);
            if (_checksum != Header.Checksum)
            {
                throw new Exception($"Discreet.Network.Core.Packet.Populate: checksum mismatch; got {Header.Checksum}, but calculated {_checksum}");
            }

            Body = DecodePacketBody(Header.Command, bodyData, 0);
        }

        public void Populate(byte[] bytes)
        {
            Header = new PacketHeader(bytes, 0);

            if ((bytes.Length - 10) < Header.Length)
            {
                throw new Exception($"Discreet.Network.Core.Packet.Populate: expected {Header.Length} bytes in payload, but got {bytes.Length - 10}");
            }

            uint _checksum = Coin.Serialization.GetUInt32(SHA256.HashData(SHA256.HashData(new Span<byte>(bytes, 10, bytes.Length - 10))), 0);
            if (_checksum != Header.Checksum)
            {
                throw new Exception($"Discreet.Network.Core.Packet.Populate: checksum mismatch; got {Header.Checksum}, but calculated {_checksum}");
            }

            Body = DecodePacketBody(Header.Command, bytes, 10);
        }

        public static IPacketBody DecodePacketBody(PacketType t, byte[] data, uint offset)
        {
            switch (t)
            {
                case PacketType.GETVERSION:
                    return new Packets.GetVersionPacket();
                case PacketType.VERSION:
                    return new Packets.VersionPacket(data, offset);
                case PacketType.INVENTORY:
                    return new Packets.InventoryPacket(data, offset);
                case PacketType.GETBLOCKS:
                    return new Packets.GetBlocksPacket(data, offset);
                case PacketType.BLOCKS:
                    return new Packets.BlocksPacket(data, offset);
                case PacketType.GETTXS:
                    return new Packets.GetTransactionsPacket(data, offset);
                case PacketType.TXS:
                    return new Packets.TransactionsPacket(data, offset);
                case PacketType.NOTFOUND:
                    return new Packets.NotFoundPacket(data, offset);
                case PacketType.REJECT:
                    return new Packets.RejectPacket(data, offset);
                case PacketType.ALERT:
                    return new Packets.AlertPacket(data, offset);
                case PacketType.SENDTX:
                    return new Packets.SendTransactionPacket(data, offset);
                case PacketType.SENDBLOCK:
                    return new Packets.SendBlockPacket(data, offset);
                case PacketType.SENDMSG:
                    return new Packets.SendMessagePacket(data, offset);
                case PacketType.NONE:
                    throw new Exception("Discreet.Network.Core.Packet.DecodePacketBody: dummy packet received (PacketType.NONE)");
                default:
                    throw new Exception($"Discreet.Network.Core.Packet.DecodePacketBody: unknown packet type {t}");
            }
        }

        public static IPacketBody DecodePacketBody(PacketType t, Stream s)
        {
            switch (t)
            {
                case PacketType.GETVERSION:
                    return new Packets.GetVersionPacket();
                case PacketType.VERSION:
                    return new Packets.VersionPacket(s);
                case PacketType.INVENTORY:
                    return new Packets.InventoryPacket(s);
                case PacketType.GETBLOCKS:
                    return new Packets.GetBlocksPacket(s);
                case PacketType.BLOCKS:
                    return new Packets.BlocksPacket(s);
                case PacketType.GETTXS:
                    return new Packets.GetTransactionsPacket(s);
                case PacketType.TXS:
                    return new Packets.TransactionsPacket(s);
                case PacketType.NOTFOUND:
                    return new Packets.NotFoundPacket(s);
                case PacketType.REJECT:
                    return new Packets.RejectPacket(s);
                case PacketType.ALERT:
                    return new Packets.AlertPacket(s);
                case PacketType.SENDTX:
                    return new Packets.SendTransactionPacket(s);
                case PacketType.SENDBLOCK:
                    return new Packets.SendBlockPacket(s);
                case PacketType.SENDMSG:
                    return new Packets.SendMessagePacket(s);
                case PacketType.NONE:
                    throw new Exception("Discreet.Network.Core.Packet.DecodePacketBody: dummy packet received (PacketType.NONE)");
                default:
                    throw new Exception($"Discreet.Network.Core.Packet.DecodePacketBody: unknown packet type {t}");
            }
        }
    }
}
