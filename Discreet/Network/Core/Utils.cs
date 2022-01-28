using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core
{
    public static class Utils
    {
        public static IPEndPoint DeserializeEndpoint(byte[] b, uint offset)
        {
            IPAddress addr;

            if (IsIPv4(new Span<byte>(b, (int)offset, 16)))
            {
                addr = new IPAddress(new Span<byte>(b, (int)offset + 12, 4));
            }
            else
            {
                addr = new IPAddress(new Span<byte>(b, (int)offset, 16));
            }

            return new IPEndPoint(addr, b[offset + 16] << 8 | b[offset + 17]);
            
        }

        public static IPEndPoint DeserializeEndpoint(Stream s)
        {
            IPAddress addr;

            byte[] _addr = new byte[16];
            s.Read(_addr);

            if (IsIPv4(_addr))
            {
                addr = new IPAddress(_addr);
            }
            else
            {
                addr = new IPAddress(_addr);
            }

            int _port = s.ReadByte() << 8;
            _port |= s.ReadByte();

            return new IPEndPoint(addr, _port);
        }

        public static bool IsIPv4(ReadOnlySpan<byte> b)
        {
            bool _isIPv4 = b[0] == 0 && b[1] == 0 && b[2] == 0 && b[3] == 0 && b[4] == 0 && b[5] == 0 && b[6] == 0 && b[7] == 0 && b[8] == 0 && b[9] == 0 && b[10] == 0xff && b[11] == 0xff;

            return _isIPv4;
        }

        public static void SerializeEndpoint(IPEndPoint p, Stream s)
        {
            if (p.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                s.Write(p.Address.GetAddressBytes()); 
            }
            else
            {
                s.Write(new byte[10]);
                s.Write(new byte[2] { 0xff, 0xff });
                s.Write(p.Address.GetAddressBytes());
            }

            s.Write(new byte[] { (byte)(p.Port >> 8), (byte)(p.Port & 0xff) });
        }

        public static void SerializeEndpoint(IPEndPoint p, byte[] b, uint offset)
        {
            if (p.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                Array.Copy(p.Address.GetAddressBytes(), 0, b, offset, 16);
            }
            else
            {
                Array.Copy(new byte[10], 0, b, offset, 10);
                b[offset + 10] = 0xff;
                b[offset + 11] = 0xff;
                Array.Copy(p.Address.GetAddressBytes(), 0, b, offset + 12, 4);
            }

            b[offset + 16] = (byte)(p.Port >> 8);
            b[offset + 17] = (byte)(p.Port & 0xff);
        }
    }
}
