﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class VersionPacket: IPacketBody
    {
        public uint Version { get; private set; }
        public ServicesFlag Services { get; private set; }
        public long Timestamp { get; private set; }
        public long Height { get; private set; }
        public IPEndPoint Address { get; private set; }
        public Cipher.SHA256 ID { get; private set; }
        public bool Syncing { get; private set; }

        public VersionPacket()
        {

        }

        public VersionPacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public VersionPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            Version = Coin.Serialization.GetUInt32(b, offset);
            offset += 4;

            Services = (ServicesFlag)Coin.Serialization.GetUInt32(b, offset);
            offset += 4;

            Timestamp = Coin.Serialization.GetInt64(b, offset);
            offset += 8;

            Height = Coin.Serialization.GetInt64(b, offset);
            offset += 8;

            Address = Utils.DeserializeEndpoint(b, offset);
            offset += 18;

            byte[] _id = new byte[32];
            Array.Copy(b, offset, _id, 0, 32);
            ID = new Cipher.SHA256(_id, false);
            offset += 32;

            Syncing = b[offset] != 0;
        }

        public void Deserialize(Stream s)
        {
            byte[] uintbuf = new byte[4];
            byte[] longbuf = new byte[8];

            s.Read(uintbuf);
            Version = Coin.Serialization.GetUInt32(uintbuf, 0);

            s.Read(uintbuf);
            Services = (ServicesFlag)Coin.Serialization.GetUInt32(uintbuf, 0);

            s.Read(longbuf);
            Timestamp = Coin.Serialization.GetInt64(longbuf, 0);

            s.Read(longbuf);
            Height = Coin.Serialization.GetInt64(longbuf, 0);

            Address = Utils.DeserializeEndpoint(s);

            byte[] _id = new byte[32];
            s.Read(_id);
            ID = new Cipher.SHA256(_id, false);

            Syncing = s.ReadByte() != 0;
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Coin.Serialization.CopyData(b, offset, Version);
            offset += 4;

            Coin.Serialization.CopyData(b, offset, (uint)Services);
            offset += 4;

            Coin.Serialization.CopyData(b, offset, Timestamp);
            offset += 8;

            Coin.Serialization.CopyData(b, offset, Height);
            offset += 8;

            Utils.SerializeEndpoint(Address, b, offset);
            offset += 18;

            Array.Copy(ID.Bytes, 0, b, offset, 32);
            offset += 32;

            b[offset] = Syncing ? (byte)1 : (byte)0;
            return offset + 1;
        }

        public void Serialize(Stream s)
        {
            s.Write(Coin.Serialization.UInt32(Version));
            s.Write(Coin.Serialization.UInt32((uint)Services));
            s.Write(Coin.Serialization.Int64(Timestamp));
            s.Write(Coin.Serialization.Int64(Height));
            Utils.SerializeEndpoint(Address, s);
            s.Write(ID.Bytes);
            s.WriteByte(Syncing ? (byte)1 : (byte)0);
        }
    }
}