using Discreet.Cipher;
using Discreet.Coin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;

namespace Discreet.Common.Serialize
{
    public static class Util
    {
        public delegate void CustomSerializer(BEBinaryWriter writer);
        public delegate void CustomDeserializer(ref MemoryReader reader);
        public delegate T DeserializeElement<T>(ref MemoryReader reader);

        public static T ReadElement<T>(this ref MemoryReader reader, DeserializeElement<T> deserializer)
        {
            return deserializer(ref reader);
        }

        // WARNING: this relies on DeserializeElement<T> on being implemented properly.
        public static T[] ReadArray<T>(this ref MemoryReader reader, DeserializeElement<T> deserializer, int length = -1)
        {
            if (length < 0) length = reader.ReadInt32();
            T[] arr = new T[length];

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = ReadElement(ref reader, deserializer);
            }
            return arr;
        }

        public static T ReadSerializable<T>(this ref MemoryReader reader, Func<T, CustomDeserializer> selector = null) where T : ISerializable, new()
        {
            T res = new();
            if (selector != null)
            {
                selector(res)(ref reader);
            }
            else
            {
                res.Deserialize(ref reader);
            }
            return res;
        }

        public static T[] ReadSerializableArray<T>(this ref MemoryReader reader, int length = -1, Func<T, CustomDeserializer> selector = null) where T : ISerializable, new()
        {
            if (length < 0) length = reader.ReadInt32();
            T[] arr = new T[length];

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = ReadSerializable(ref reader, selector);
            }
            return arr;
        }

        public static T AsSerializable<T>(this byte[] data, int offset = 0) where T : ISerializable, new()
        {
            MemoryReader reader = new(data.AsMemory(offset));
            return reader.ReadSerializable<T>();
        }

        public static T AsSerializable<T>(this ReadOnlyMemory<byte> data) where T : ISerializable, new()
        {
            if (data.IsEmpty) throw new FormatException();
            MemoryReader reader = new(data);
            return reader.ReadSerializable<T>();
        }

        public static ISerializable AsSerializable(this ReadOnlyMemory<byte> data, Type type)
        {
            if (!typeof(ISerializable).GetTypeInfo().IsAssignableFrom(type))
                throw new InvalidCastException();
            ISerializable res = (ISerializable)Activator.CreateInstance(type);
            MemoryReader reader = new(data);
            res.Deserialize(ref reader);
            return res;
        }

        public static T[] AsSerializableArray<T>(this byte[] data, int offset = 0) where T : ISerializable, new()
        {
            MemoryReader reader = new(data.AsMemory(offset));
            return reader.ReadSerializableArray<T>();
        }

        public static Cipher.SHA256 ReadSHA256(this ref MemoryReader reader)
        {
            return new Cipher.SHA256(reader.ReadMemory(32).ToArray(), false);
        }

        public static Cipher.Key ReadKey(this ref MemoryReader reader)
        {
            return new Cipher.Key(reader.ReadMemory(32).ToArray());
        }

        public static Cipher.Key[] ReadKeyArray(this ref MemoryReader reader, int len = -1)
        {
            if (len < 0) len = reader.ReadInt32();
            var keys = new Cipher.Key[len];
            for (int i = 0; i < len; i++)
            {
                keys[i] = reader.ReadKey();
            }

            return keys;
        }

        public static Cipher.SHA256[] ReadSHA256Array(this ref MemoryReader reader, int len = -1)
        {
            if (len < 0) len = reader.ReadInt32();
            var hashes = new Cipher.SHA256[len];
            for (int i = 0; i < len; i++)
            {
                hashes[i] = reader.ReadSHA256();
            }

            return hashes;
        }

        public static byte[] ReadByteArray(this ref MemoryReader reader, int len = -1)
        {
            if (len < 0) len = reader.ReadInt32();
            return reader.ReadMemory(len).ToArray();
        }

        public static uint[] ReadUInt32Array(this ref MemoryReader reader, int len = -1)
        {
            if (len < 0) len = reader.ReadInt32();
            uint[] result = new uint[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = reader.ReadUInt32();
            }

            return result;
        }

        public static void WriteElement<T>(this BEBinaryWriter writer, T elem, Action<T, BEBinaryWriter> serializer)
        {
            serializer(elem, writer);
        }

        public static void WriteArray<T>(this BEBinaryWriter writer, T[] arr, Action<T, BEBinaryWriter> serializer, bool writeLength = true)
        {
            if (arr == null)
            {
                if (writeLength) writer.Write(0);
                return;
            }

            if (writeLength) writer.Write(arr.Length);

            for (int i = 0; i < arr.Length; i++)
            {
                serializer(arr[i], writer);
            }
        }

        public static void WriteSerializableArray<T>(this BEBinaryWriter writer, T[] arr, bool writeLength = true, Func<T, CustomSerializer> selector = null) where T : ISerializable
        {
            if (arr == null)
            {
                if (writeLength) writer.Write(0);
                return;
            }

            if (writeLength) writer.Write(arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                if (selector is null)
                {
                    arr[i].Serialize(writer);
                }
                else
                {
                    selector(arr[i])(writer);
                }
            }
        }

        public static void WriteSerializableCollection<T>(this BEBinaryWriter writer, ICollection<T> col, bool writeCount = true) where T : ISerializable
        {
            if (writeCount) writer.Write(col.Count);
            foreach (var item in col)
            {
                item.Serialize(writer);
            }
        }

        public static void WriteSHA256(this BEBinaryWriter writer, SHA256 hash)
        {
            writer.Write(hash.Bytes);
        }

        public static void WriteKey(this BEBinaryWriter writer, Key key)
        {
            writer.Write(key.bytes);
        }

        public static void WriteKeyArray(this BEBinaryWriter writer, Key[] keys, bool writeLength = true)
        {
            if (keys == null)
            {
                if (writeLength) writer.Write(0);
                return;
            }
            if (writeLength) writer.Write(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                writer.Write(keys[i].bytes);
            }
        }

        public static void WriteSHA256Array(this BEBinaryWriter writer, SHA256[] hashes, bool writeLength = true)
        {
            if (hashes == null)
            {
                if (writeLength) writer.Write(0);
                return;
            }
            if (writeLength) writer.Write(hashes.Length);
            for (int i = 0; i < hashes.Length; i++)
            {
                writer.Write(hashes[i].Bytes);
            }
        }

        public static void Write<T>(this BEBinaryWriter writer, T value) where T : ISerializable
        {
            value.Serialize(writer);
        }

        public static void WriteByteArray(this BEBinaryWriter writer, byte[] bytes, bool writeLength = true)
        {
            if (bytes == null)
            {
                if (writeLength) writer.Write(0);
                return;
            }
            if (writeLength) writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        public static void WriteUInt32Array(this BEBinaryWriter writer, uint[] value, bool writeLength = true)
        {
            if (value == null)
            {
                if (writeLength) writer.Write(0);
                return;
            }
            if (writeLength) writer.Write(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                writer.Write(value[i]);
            }
        }

        public static byte[] Serialize<T>(this T obj) where T : ISerializable
        {
            var rv = new byte[obj.Size];
            var writer = new BEBinaryWriter(new MemoryStream(rv));
            obj.Serialize(writer);
            return rv;
        }

        public static void Serialize<T>(this T obj, Stream s) where T : ISerializable
        {
            obj.Serialize(new BEBinaryWriter(s));
        }

        public static int Serialize<T>(this T obj, byte[] data, int offset = 0) where T : ISerializable
        {
            obj.Serialize(new BEBinaryWriter(new MemoryStream(data, offset, data.Length - offset)));
            return offset + obj.Size;
        }

        public static void Deserialize<T>(this T obj, MemoryStream s) where T : ISerializable
        {
            MemoryReader reader = new(s.ToArray());
            obj.Deserialize(ref reader);
        }

        public static void Deserialize<T>(this T obj, byte[] data, int offset = 0) where T : ISerializable
        {
            MemoryReader reader = new(new ReadOnlyMemory<byte>(data, offset, data.Length - offset));
            obj.Deserialize(ref reader);
        }

        public static SHA256 Hash<T>(this T obj) where T : IHashable
        {
            return SHA256.HashData(obj.Serialize());
        }
    }
}
