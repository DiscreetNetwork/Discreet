using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom.Protocol.Common
{
    public class ReadPacketBase
    {
        private byte[] _bytes = new byte[0];
        int _readPosition = 0;

        public ReadPacketBase(byte[] bytes)
        {
            _bytes = bytes;
        }

        public int ReadInt()
        {
            int value = BitConverter.ToInt32(_bytes, _readPosition);
            _readPosition += 4;
            return value;
        }

        public float ReadFloat()
        {
            float value = BitConverter.ToSingle(_bytes, _readPosition);
            _readPosition += 4;
            return value;
        }

        public bool ReadBoolean()
        {
            bool value = BitConverter.ToBoolean(_bytes, _readPosition);
            _readPosition += 1;
            return value;
        }

        public string ReadString()
        {
            int stringLength = ReadInt();
            string value = Encoding.UTF8.GetString(_bytes, _readPosition, stringLength);
            _readPosition += stringLength;
            return value;
        }

        public byte[] ReadBytes()
        {
            int bytesLength = ReadInt();
            byte[] bytes = _bytes.Skip(_readPosition).Take(bytesLength).ToArray();
            _readPosition += bytesLength;
            return bytes;
        }

        public byte[] ReadBytes(int num)
        {
            byte[] bytes = _bytes.Skip(_readPosition).Take(num).ToArray();
            _readPosition += num;
            return bytes;
        }

        public BigInteger ReadBigInteger()
        {
            return new BigInteger(ReadBytes());
        }

        public Cipher.Key ReadKey()
        {
            var rv = new Cipher.Key(_bytes.Skip(_readPosition).Take(32).ToArray());
            _readPosition += 32;

            return rv;
        }
    }
}
