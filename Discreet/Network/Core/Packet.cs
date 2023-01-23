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

        public Packet(PacketType type, IPacketBody body)
        {
            Header = new PacketHeader(type, body);
            Body = body;
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

            if (Header.NetworkID != Daemon.DaemonConfig.GetConfig().NetworkID)
            {
                throw new Exception($"Discreet.Network.Core.Packet.Populate: wrong network ID; expected {Daemon.DaemonConfig.GetConfig().NetworkID} but got {Header.NetworkID}");
            }

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

            if (Header.NetworkID != Daemon.DaemonConfig.GetConfig().NetworkID)
            {
                throw new Exception($"Discreet.Network.Core.Packet.Populate: wrong network ID; expected {Daemon.DaemonConfig.GetConfig().NetworkID} but got {Header.NetworkID}");
            }

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

        public byte[] Serialize()
        {
            byte[] bytes = new byte[Header.Length + 10];
            Header.Encode(bytes, 0);
            Body.Serialize(bytes, 10);

            return bytes;
        }

        public static IPacketBody DecodePacketBody(PacketType t, byte[] data, uint offset)
        {
            switch (t)
            {
                case PacketType.VERSION:
                    return new Packets.Peerbloom.VersionPacket(data, offset);
                case PacketType.VERACK:
                    return new Packets.Peerbloom.VerAck(data, offset);
                case PacketType.INVENTORY:
                    return new Packets.InventoryPacket(data, offset);
                case PacketType.GETBLOCKS:
                    return new Packets.GetBlocksPacket(data, offset);
                case PacketType.BLOCKS:
                    return new Packets.BlocksPacket(data, offset);
                case PacketType.GETHEADERS:
                    return new Packets.GetHeadersPacket(data, offset);
                case PacketType.HEADERS:
                    return new Packets.HeadersPacket(data, offset);
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
                case PacketType.GETPOOL:
                    return new Packets.GetPoolPacket(data, offset);
                case PacketType.POOL:
                    return new Packets.PoolPacket(data, offset);
                case PacketType.REQUESTPEERS:
                    return new Packets.Peerbloom.RequestPeers(data, offset);
                case PacketType.REQUESTPEERSRESP:
                    return new Packets.Peerbloom.RequestPeersResp(data, offset);
                case PacketType.NETPING:
                    return new Packets.Peerbloom.NetPing(data, offset);
                case PacketType.NETPONG:
                    return new Packets.Peerbloom.NetPong(data, offset);
                case PacketType.OLDMESSAGE:
                    return new Packets.Peerbloom.OldMessage(data, offset);
                case PacketType.DISCONNECT:
                    return new Packets.Peerbloom.Disconnect(data, offset);
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
                case PacketType.VERSION:
                    return new Packets.Peerbloom.VersionPacket(s);
                case PacketType.VERACK:
                    return new Packets.Peerbloom.VerAck(s);
                case PacketType.INVENTORY:
                    return new Packets.InventoryPacket(s);
                case PacketType.GETBLOCKS:
                    return new Packets.GetBlocksPacket(s);
                case PacketType.BLOCKS:
                    return new Packets.BlocksPacket(s);
                case PacketType.GETHEADERS:
                    return new Packets.GetHeadersPacket(s);
                case PacketType.HEADERS:
                    return new Packets.HeadersPacket(s);
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
                case PacketType.GETPOOL:
                    return new Packets.GetPoolPacket(s);
                case PacketType.POOL:
                    return new Packets.PoolPacket(s);
                case PacketType.REQUESTPEERS:
                    return new Packets.Peerbloom.RequestPeers(s);
                case PacketType.REQUESTPEERSRESP:
                    return new Packets.Peerbloom.RequestPeersResp(s);
                case PacketType.NETPING:
                    return new Packets.Peerbloom.NetPing(s);
                case PacketType.NETPONG:
                    return new Packets.Peerbloom.NetPong(s);
                case PacketType.OLDMESSAGE:
                    return new Packets.Peerbloom.OldMessage(s);
                case PacketType.DISCONNECT:
                    return new Packets.Peerbloom.Disconnect(s);
                case PacketType.NONE:
                    throw new Exception("Discreet.Network.Core.Packet.DecodePacketBody: dummy packet received (PacketType.NONE)");
                default:
                    throw new Exception($"Discreet.Network.Core.Packet.DecodePacketBody: unknown packet type {t}");
            }
        }
    }
}
