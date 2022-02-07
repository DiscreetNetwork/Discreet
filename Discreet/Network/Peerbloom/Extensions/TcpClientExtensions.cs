using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Discreet.Network.Peerbloom.Extensions
{
    public static class TcpClientExtensions
    {
        public static async Task<byte[]> ReadBytesAsync(this TcpClient client)
        {
            var ns = client.GetStream();
            //byte[] packetLengthBuffer = new byte[4];
            //await ns.ReadAsync(packetLengthBuffer, 0, 4);

            //int packetLength = BitConverter.ToInt32(packetLengthBuffer);

            //int totalBytesRead = 0;
            //byte[] dataBuffer = new byte[0];
            using var _ms = new MemoryStream();

            byte[] buf = new byte[1024];
            int bytesRead;

            do
            {
                bytesRead = await ns.ReadAsync(buf, 0, buf.Length);
                _ms.Write(buf, 0, bytesRead);
            } while (bytesRead == buf.Length);

            return _ms.ToArray();
        }
    }
}
