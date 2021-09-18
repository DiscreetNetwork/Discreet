using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Coin
{
    public static class Serialization
    {
        public static void CopyData(byte[] bytes, uint offset, int value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            Array.Copy(data, 0, bytes, offset, sizeof(int));
        }

        public static void CopyData(byte[] bytes, uint offset, uint value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            Array.Copy(data, 0, bytes, offset, sizeof(uint));
        }

        public static void CopyData(byte[] bytes, uint offset, long value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            Array.Copy(data, 0, bytes, offset, sizeof(long));
        }

        public static void CopyData(byte[] bytes, uint offset, ulong value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            Array.Copy(data, 0, bytes, offset, sizeof(ulong));
        }

        public static byte[] Int32(int value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return data;
        }

        public static byte[] UInt32(uint value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return data;
        }

        public static byte[] Int64(long value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return data;
        }

        public static byte[] UInt64(ulong value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return data;
        }

        public static int GetInt32(byte[] bytes, uint offset)
        {
            byte[] data = new byte[sizeof(int)];
            Array.Copy(bytes, offset, data, 0, sizeof(int));

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToInt32(data);
        }

        public static uint GetUInt32(byte[] bytes, uint offset)
        {
            byte[] data = new byte[sizeof(uint)];
            Array.Copy(bytes, offset, data, 0, sizeof(uint));

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToUInt32(data);
        }

        public static long GetInt64(byte[] bytes, uint offset)
        {
            byte[] data = new byte[sizeof(long)];
            Array.Copy(bytes, offset, data, 0, sizeof(long));

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToInt64(data);
        }

        public static ulong GetUInt64(byte[] bytes, uint offset)
        {
            byte[] data = new byte[sizeof(ulong)];
            Array.Copy(bytes, offset, data, 0, sizeof(ulong));

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToUInt64(data);
        }
    }
}
