using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using Discreet.Common.Serialize;

namespace Discreet.Network.Core
{
    public class Packet : ISerializable
    {
        public PacketHeader Header { get; set; }

        public IPacketBody Body { get; set; }

        public int Size => Header.Size + Body.Size;

        public Packet() { }

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

        public void Serialize(BEBinaryWriter writer)
        {
            Header.Serialize(writer);
            Body.Serialize(writer);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Header = reader.ReadSerializable<PacketHeader>();
            Body = DecodePacketBody(Header.Command, ref reader);
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

            uint _checksum = Common.Serialization.GetUInt32(SHA256.HashData(SHA256.HashData(new Span<byte>(bytes, 10, bytes.Length - 10))), 0);
            if (_checksum != Header.Checksum)
            {
                throw new Exception($"Discreet.Network.Core.Packet.Populate: checksum mismatch; got {Header.Checksum}, but calculated {_checksum}");
            }

            var reader = new MemoryReader(bytes.AsMemory(10));
            Body = DecodePacketBody(Header.Command, ref reader);
        }

        public static IPacketBody DecodePacketBody(PacketType t, ref MemoryReader reader)
        {
            return t switch
            {
                PacketType.VERSION => reader.ReadSerializable<Packets.Peerbloom.VersionPacket>(),
                PacketType.VERACK => reader.ReadSerializable<Packets.Peerbloom.VerAck>(),
                PacketType.INVENTORY => reader.ReadSerializable<Packets.InventoryPacket>(),
                PacketType.GETBLOCKS => reader.ReadSerializable<Packets.GetBlocksPacket>(),
                PacketType.BLOCKS => reader.ReadSerializable<Packets.BlocksPacket>(),
                PacketType.GETHEADERS => reader.ReadSerializable<Packets.GetHeadersPacket>(),
                PacketType.HEADERS => reader.ReadSerializable<Packets.HeadersPacket>(),
                PacketType.GETTXS => reader.ReadSerializable<Packets.GetTransactionsPacket>(),
                PacketType.TXS => reader.ReadSerializable<Packets.TransactionsPacket>(),
                PacketType.NOTFOUND => reader.ReadSerializable<Packets.NotFoundPacket>(),
                PacketType.REJECT => reader.ReadSerializable<Packets.RejectPacket>(),
                PacketType.ALERT => reader.ReadSerializable<Packets.AlertPacket>(),
                PacketType.SENDTX => reader.ReadSerializable<Packets.SendTransactionPacket>(),
                PacketType.SENDBLOCK => reader.ReadSerializable<Packets.SendBlockPacket>(),
                PacketType.SENDMSG => reader.ReadSerializable<Packets.SendMessagePacket>(),
                PacketType.GETPOOL => reader.ReadSerializable<Packets.GetPoolPacket>(),
                PacketType.POOL => reader.ReadSerializable<Packets.PoolPacket>(),
                PacketType.REQUESTPEERS => reader.ReadSerializable<Packets.Peerbloom.RequestPeers>(),
                PacketType.REQUESTPEERSRESP => reader.ReadSerializable<Packets.Peerbloom.RequestPeersResp>(),
                PacketType.NETPING => reader.ReadSerializable<Packets.Peerbloom.NetPing>(),
                PacketType.NETPONG => reader.ReadSerializable<Packets.Peerbloom.NetPong>(),
                PacketType.OLDMESSAGE => reader.ReadSerializable<Packets.Peerbloom.OldMessage>(),
                PacketType.DISCONNECT => reader.ReadSerializable<Packets.Peerbloom.Disconnect>(),
                PacketType.SENDBLOCKS => reader.ReadSerializable<Packets.SendBlocksPacket>(),
                PacketType.NONE => throw new Exception("Discreet.Network.Core.Packet.DecodePacketBody: dummy packet received (PacketType.NONE)"),
                _ => throw new Exception($"Discreet.Network.Core.Packet.DecodePacketBody: unknown packet type {t}"),
            };
        }
    }
}
