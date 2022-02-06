using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom.Protocol.Common
{
    public class WritePacketBase
    {
        private byte[] _bytes = new byte[0];

        /// <summary>
        /// Prepares the packet by inserting the packet length at the beginning, and returning the byte array
        /// </summary>
        /// <returns></returns>
        public byte[] ToNetworkByteArray()
        {
            byte[] dataLengthBuffer = BitConverter.GetBytes(_bytes.Length);
            byte[] newBytes = new byte[_bytes.Length + dataLengthBuffer.Length];
            Array.Copy(dataLengthBuffer, 0, newBytes, 0, 4);
            Array.Copy(_bytes, 0, newBytes, 4, _bytes.Length);

            return newBytes;
        }

        public void WriteInt(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            byte[] newBytes = new byte[_bytes.Length + bytes.Length];
            Array.Copy(_bytes, 0, newBytes, 0, _bytes.Length);
            Array.Copy(bytes, 0, newBytes, _bytes.Length, bytes.Length);
            _bytes = newBytes;
        }

        public void WriteFloat(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            byte[] newBytes = new byte[_bytes.Length + bytes.Length];
            Array.Copy(_bytes, 0, newBytes, 0, _bytes.Length);
            Array.Copy(bytes, 0, newBytes, _bytes.Length, bytes.Length);
            _bytes = newBytes;
        }

        public void WriteBoolean(bool value)
        {
            var bytes = BitConverter.GetBytes(value);
            byte[] newBytes = new byte[_bytes.Length + bytes.Length];
            Array.Copy(_bytes, 0, newBytes, 0, _bytes.Length);
            Array.Copy(bytes, 0, newBytes, _bytes.Length, bytes.Length);
            _bytes = newBytes;
        }

        public void WriteString(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte[] newBytes = new byte[_bytes.Length + bytes.Length + 4];
            Array.Copy(_bytes, 0, newBytes, 0, _bytes.Length);
            Array.Copy(BitConverter.GetBytes(bytes.Length), 0, newBytes, _bytes.Length, 4);
            Array.Copy(bytes, 0, newBytes, _bytes.Length + 4, bytes.Length);
            _bytes = newBytes;
        }

        public void WriteBytes(byte[] value)
        {
            WriteInt(value.Length);
            byte[] newBytes = new byte[_bytes.Length + value.Length];
            Array.Copy(_bytes, 0, newBytes, 0, _bytes.Length);
            Array.Copy(value, 0, newBytes, _bytes.Length, value.Length);
            _bytes = newBytes;
        }

        public void WriteBigInteger(BigInteger value)
        {
            WriteBytes(value.ToByteArray());
        }

        public void WriteKey(Cipher.Key value)
        {
            byte[] newBytes = new byte[_bytes.Length + 32];
            Array.Copy(_bytes, 0, newBytes, 0, _bytes.Length);
            Array.Copy(value.bytes, 0, newBytes, _bytes.Length, 32);
            _bytes = newBytes;
        }
    }
}
