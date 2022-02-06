using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets.Peerbloom
{
    public struct FindNodeRespElem
    {
        public Cipher.Key ID;
        public IPEndPoint Endpoint;

        public FindNodeRespElem(Network.Peerbloom.RemoteNode node)
        {
            ID = node.Id.Value;
            Endpoint = node.Endpoint;
        }
    }

    public class FindNodeResp: IPacketBody
    {
        public int Length { get; set; }
        public FindNodeRespElem[] Elems { get; set; }

        public FindNodeResp() { }

        public FindNodeResp(Stream s)
        {
            Deserialize(s);
        }

        public FindNodeResp(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            Length = Coin.Serialization.GetInt32(b, offset);
            offset += 4;

            Elems = new FindNodeRespElem[Length];

            for (int i = 0; i < Length; i++)
            {
                Elems[i] = new FindNodeRespElem { ID = new Cipher.Key(new byte[32]) };
                Array.Copy(b, offset, Elems[i].ID.bytes, 0, 32);
                offset += 32;
                Elems[i].Endpoint = Utils.DeserializeEndpoint(b, offset);
                offset += 18;
            }
        }

        public void Deserialize(Stream s)
        {
            byte[] uintbuf = new byte[4];
            s.Read(uintbuf);
            Length = Coin.Serialization.GetInt32(uintbuf, 0);

            Elems = new FindNodeRespElem[Length];

            for (int i = 0; i < Length; i++)
            {
                Elems[i] = new FindNodeRespElem { ID = new Cipher.Key(new byte[32]) };
                s.Read(Elems[i].ID.bytes);
                Elems[i].Endpoint = Utils.DeserializeEndpoint(s);
            }
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Coin.Serialization.CopyData(b, offset, Length);
            offset += 4;

            foreach (var elem in Elems)
            {
                Array.Copy(elem.ID.bytes, 0, b, offset, 32);
                offset += 32;
                Utils.SerializeEndpoint(elem.Endpoint, b, offset);
                offset += 18;
            }

            return offset;
        }

        public void Serialize(Stream s)
        {
            s.Write(Coin.Serialization.Int32(Length));

            foreach (var elem in Elems)
            {
                s.Write(elem.ID.bytes);
                Utils.SerializeEndpoint(elem.Endpoint, s);
            }
        }

        public int Size()
        {
            return 4 + Elems.Length * 50;
        }
    }
}
