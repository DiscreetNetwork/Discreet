using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discreet.Network.Core;
using System.Security.Cryptography;

namespace Discreet.Network.Peerbloom.Extensions
{
    public static class TcpClientExtensions
    {
        public static async Task<byte[]> ReadBytesAsync(this TcpClient client)
        {
            var ns = client.GetStream();
            
            

            using var _ms = new MemoryStream();

            //TODO: fix buffer
            byte[] buf = new byte[1048576];
            int bytesRead = 0;

            do
            {
                try
                {
                    bytesRead = await ns.ReadAsync(buf, 0, buf.Length);
                }
                catch (Exception e)
                {
                    Daemon.Logger.Error("ReadBytesAsync error: " + e.Message, e);
                }
                _ms.Write(buf, 0, bytesRead);
            } while (bytesRead == buf.Length);

            return _ms.ToArray();
        }

        public static async Task<Packet> ReadPacketAsync(this TcpClient client)
        {
            var ns = client.GetStream();

            var timeout = DateTime.Now.AddSeconds(5);
            while (timeout.Ticks > DateTime.Now.Ticks && !ns.DataAvailable)
            {

            }

            try
            {
                byte[] _headerBytes = new byte[10];
                var _numBytes = await ns.ReadAsync(_headerBytes, 0, 10);

                while (_numBytes == 0)
                {
                    _numBytes = await ns.ReadAsync(_headerBytes, 0, 10);
                }

                PacketHeader Header = new PacketHeader(_headerBytes);

                if (Header.NetworkID != Daemon.DaemonConfig.GetConfig().NetworkID)
                {
                    throw new Exception($"wrong network ID; expected {Daemon.DaemonConfig.GetConfig().NetworkID} but got {Header.NetworkID}");
                }

                if ((Header.Length + 10) > Constants.MAX_PEERBLOOM_PACKET_SIZE)
                {
                    throw new Exception($"Received packet was larger than allowed {Constants.MAX_PEERBLOOM_PACKET_SIZE} bytes.");
                }

                byte[] _bytes = new byte[Header.Length];

                
                int _numRead;
                int _offset = 0;

                do
                {
                    _numRead = await ns.ReadAsync(_bytes, _offset, _bytes.Length - _offset);
                    _offset += _numRead;
                } while (_offset < Header.Length && DateTime.Now.Ticks < timeout.Ticks && _numRead > 0);
                
                if (_offset < Header.Length)
                {
                    throw new Exception($"ReadPacketAsync: expected {Header.Length} bytes in payload, but got {_numRead}");
                }

                uint _checksum = Common.Serialization.GetUInt32(SHA256.HashData(SHA256.HashData(_bytes)), 0);
                if (_checksum != Header.Checksum)
                {
                    throw new Exception($"ReadPacketAsync: checksum mismatch; got {Header.Checksum}, but calculated {_checksum}");
                }

                Packet p = new Packet();

                p.Header = Header;
                p.Body = Packet.DecodePacketBody(Header.Command, _bytes, 0);

                return p;
            }
            catch (Exception ex)
            {
                //await ns.FlushAsync();

                throw ex;
            }
        }
    }
}
