using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class GetHeadersPacket : IPacketBody
    {
        public long StartingHeight { get; set; }
        public uint Count { get; set; }
        public Cipher.SHA256[] Headers { get; set; }

        public GetHeadersPacket()
        {

        }

        public GetHeadersPacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public GetHeadersPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            StartingHeight = Common.Serialization.GetInt64(b, offset);
            offset += 8;

            Count = Common.Serialization.GetUInt32(b, offset);
            offset += 4;

            int headersLen = Common.Serialization.GetInt32(b, offset);
            offset += 4;

            Headers = new Cipher.SHA256[headersLen];

            for (int i = 0; i < headersLen; i++)
            {
                Headers[i] = new Cipher.SHA256(b, offset);
                offset += 32;
            }
        }

        public void Deserialize(Stream s)
        {
            StartingHeight = Common.Serialization.GetInt64(s);
            Count = Common.Serialization.GetUInt32(s);

            int headersLen = Common.Serialization.GetInt32(s);
            Headers = new Cipher.SHA256[headersLen];

            for (int i = 0; i < headersLen; i++)
            {
                Headers[i] = new Cipher.SHA256(s);
            }
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Common.Serialization.CopyData(b, offset, StartingHeight);
            offset += 8;

            Common.Serialization.CopyData(b, offset, Count);
            offset += 4;

            Common.Serialization.CopyData(b, offset, Headers == null ? 0 : Headers.Length);
            offset += 4;

            if (Headers != null)
            {
                foreach (Cipher.SHA256 h in Headers)
                {
                    Array.Copy(h.Bytes, 0, b, offset, 32);
                    offset += 32;
                }
            }

            return offset;
        }

        public void Serialize(Stream s)
        {
            Common.Serialization.CopyData(s, StartingHeight);
            Common.Serialization.CopyData(s, Count);
            Common.Serialization.CopyData(s, Headers == null ? 0 : Headers.Length);

            if (Headers != null)
            {
                foreach (Cipher.SHA256 h in Headers)
                {
                    s.Write(h.Bytes);
                }
            }
        }

        public int Size()
        {
            return 16 + ((Headers == null) ? 0 : 32 * Headers.Length);
        }
    }
}
