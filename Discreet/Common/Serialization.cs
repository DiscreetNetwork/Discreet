using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Discreet.Common
{
    public static class Serialization
    {
        public static byte[] UInt32Array(uint[] arr)
        {
            byte[] bytes = new byte[arr.Length * 4 + 4];
            CopyData(bytes, 0, arr.Length);

            for (int i = 0; i < arr.Length; i++)
            {
                CopyData(bytes, (uint)(i + 1) * 4, arr[i]);
            }

            return bytes;
        }

        public static uint[] GetUInt32Array(byte[] bytes)
        {
            uint[] arr = new uint[bytes.Length / 4 - 1];

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = GetUInt32(bytes, (uint)(i + 1) * 4);
            }

            return arr;
        }

        public static void CopyData(byte[] bytes, uint offset, short value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            Array.Copy(data, 0, bytes, offset, sizeof(short));
        }

        public static void CopyData(byte[] bytes, uint offset, ushort value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            Array.Copy(data, 0, bytes, offset, sizeof(ushort));
        }

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

        public static void CopyData(byte[] bytes, uint offset, float value)
        {
            byte[] data = BitConverter.GetBytes(value);

            Array.Copy(data, 0, bytes, offset, sizeof(float));
        }

        public static byte[] Int16(short value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return data;
        }

        public static byte[] UInt16(ushort value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return data;
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

        public static short GetInt16(byte[] bytes, uint offset)
        {
            byte[] data = new byte[sizeof(short)];
            Array.Copy(bytes, offset, data, 0, sizeof(short));

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToInt16(data);
        }

        public static ushort GetUInt16(byte[] bytes, uint offset)
        {
            byte[] data = new byte[sizeof(ushort)];
            Array.Copy(bytes, offset, data, 0, sizeof(ushort));

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToUInt16(data);
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

        public static void CopyData(Stream s, short value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            s.Write(data);
        }

        public static void CopyData(Stream s, ushort value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            s.Write(data);
        }

        public static void CopyData(Stream s, int value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            s.Write(data);
        }

        public static void CopyData(Stream s, uint value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            s.Write(data);
        }

        public static void CopyData(Stream s, long value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            s.Write(data);
        }

        public static void CopyData(Stream s, ulong value)
        {
            byte[] data = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            s.Write(data);
        }

        public static short GetInt16(Stream s)
        {
            byte[] data = new byte[sizeof(short)];
            s.Read(data);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToInt16(data);
        }

        public static ushort GetUInt16(Stream s)
        {
            byte[] data = new byte[sizeof(ushort)];
            s.Read(data);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToUInt16(data);
        }

        public static int GetInt32(Stream s)
        {
            byte[] data = new byte[sizeof(int)];
            s.Read(data);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToInt32(data);
        }

        public static uint GetUInt32(Stream s)
        {
            byte[] data = new byte[sizeof(uint)];
            s.Read(data);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToUInt32(data);
        }

        public static long GetInt64(Stream s)
        {
            byte[] data = new byte[sizeof(long)];
            s.Read(data);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToInt64(data);
        }

        public static ulong GetUInt64(Stream s)
        {
            byte[] data = new byte[sizeof(ulong)];
            s.Read(data);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToUInt64(data);
        }

        public static string GetString(Stream s)
        {
            int length = GetInt32(s);
            byte[] data = new byte[length];

            s.Read(data, 0, length);

            return Encoding.UTF8.GetString(data);
        }

        public static (uint, string) GetString(byte[] bytes, uint offset)
        {
            int len = GetInt32(bytes, offset);

            offset += 4;

            return (offset + (uint)len, Encoding.UTF8.GetString(bytes, (int)offset, len));
        }

        public static void CopyData(byte[] bytes, uint offset, string str)
        {
            byte[] strBytes = Encoding.UTF8.GetBytes(str);

            CopyData(bytes, offset, strBytes.Length);
            offset += 4;
            Array.Copy(strBytes, 0, bytes, offset, strBytes.Length);
        }

        public static void CopyData(Stream s, string str)
        {
            if (str == null) str = "";

            byte[] bytes = Encoding.UTF8.GetBytes(str);

            s.Write(Int32(bytes.Length));
            s.Write(bytes);
        }

        public static void CopyData(byte[] bytes, uint offset, bool b)
        {
            bytes[offset] = b ? (byte)1 : (byte)0;
        }

        public static void CopyData(Stream s, bool b)
        {
            s.WriteByte(b ? (byte)1 : (byte)0);
        }

        public static byte[] Bool(bool b)
        {
            return new byte[] { b ? (byte)1 : (byte)0 };
        }

        public static bool GetBool(byte[] bytes, uint offset)
        {
            return bytes[offset] == 1;
        }

        public static bool GetBool(Stream s)
        {
            return s.ReadByte() == 1;
        }

        public static void CopyData(byte[] bytes, uint offset, byte[] data)
        {
            CopyData(bytes, offset, data.Length);
            offset += 4;

            Array.Copy(data, 0, bytes, offset, data.Length);
        }

        public static void CopyData(Stream s, byte[] data)
        {
            CopyData(s, data.Length);

            s.Write(data);
        }

        public static (uint, byte[]) GetBytes(byte[] bytes, uint offset)
        {
            int len = GetInt32(bytes, offset);

            offset += 4;

            byte[] data = new byte[len];
            Array.Copy(bytes, offset, data, 0, len);

            return (offset + (uint)len, data);
        }

        public static (uint, byte[]) GetBytes(Stream s)
        {
            int length = GetInt32(s);
            byte[] data = new byte[length];

            s.Read(data, 0, length);

            return ((uint)length, data);
        }
    }
}
