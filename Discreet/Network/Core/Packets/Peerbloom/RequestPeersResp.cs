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
        public IPEndPoint Endpoint;

        public FindNodeRespElem(IPEndPoint node)
        {
            Endpoint = node;
        }
    }

    public class RequestPeersResp: IPacketBody
    {
        public int Length { get; set; }
        public FindNodeRespElem[] Elems { get; set; }

        public RequestPeersResp() { }

        public RequestPeersResp(Stream s)
        {
            Deserialize(s);
        }

        public RequestPeersResp(byte[] b, uint offset)
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
                Elems[i].Endpoint = Utils.DeserializeEndpoint(s);
            }
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Coin.Serialization.CopyData(b, offset, Length);
            offset += 4;

            foreach (var elem in Elems)
            {
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
                Utils.SerializeEndpoint(elem.Endpoint, s);
            }
        }

        public int Size()
        {
            return 4 + Elems.Length * 18;
        }
    }
}
