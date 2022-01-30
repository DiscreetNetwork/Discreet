using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom.Extensions
{
    public static class TcpClientExtensions
    {
        public static async Task<byte[]> ReadBytesAsync(this TcpClient client)
        {
            var ns = client.GetStream();
            byte[] packetLengthBuffer = new byte[4];
            await ns.ReadAsync(packetLengthBuffer, 0, 4);

            int packetLength = BitConverter.ToInt32(packetLengthBuffer);

            int totalBytesRead = 0;
            byte[] dataBuffer = new byte[0];
            while (totalBytesRead != packetLength)
            {
                byte[] readBuffer = new byte[1024];
                int bytesRead = await ns.ReadAsync(readBuffer, 0, readBuffer.Length);

                byte[] temp = new byte[dataBuffer.Length + bytesRead];
                Array.Copy(dataBuffer, 0, temp, 0, dataBuffer.Length); // Copy existing / already read data to the temp buffer
                Array.Copy(readBuffer, 0, temp, dataBuffer.Length, bytesRead); // Copy the newly read data to the temp buffer
                dataBuffer = temp; // Replace the existing buffer with the updated temp buffer
                totalBytesRead += bytesRead;
            }

            return dataBuffer;
        }
    }
}
