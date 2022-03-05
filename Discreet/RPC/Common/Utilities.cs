using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

namespace Discreet.RPC.Common
{
    public class Utilities
    {
        public static object ConvertType(Type type, JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                if (type == typeof(Coin.Block))                         return Readable.Block.FromReadable(jsonElement.GetRawText());
                if (type == typeof(Coin.Transaction))                   return Readable.Transaction.FromReadable(jsonElement.GetRawText());
                if (type == typeof(Coin.FullTransaction))               return Readable.FullTransaction.FromReadable(jsonElement.GetRawText());
                if (type == typeof(Coin.MixedTransaction))              return Readable.MixedTransaction.FromReadable(jsonElement.GetRawText());
                if (type == typeof(Coin.Transparent.Transaction))       return Readable.Transparent.Transaction.FromReadable(jsonElement.GetRawText());
            }
            else if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                Array arr = (Array)Activator.CreateInstance(type, new object[] { jsonElement.GetArrayLength() });

                int i = 0;

                foreach (var obj in jsonElement.EnumerateArray())
                {
                    arr.SetValue(ConvertType(type.GetElementType(), obj), i++);
                }

                return arr;
            }
            else if (jsonElement.ValueKind == JsonValueKind.Number)
            {
                if (type == typeof(sbyte))          return jsonElement.GetSByte();
                if (type == typeof(byte))           return jsonElement.GetByte();
                if (type == typeof(short))          return jsonElement.GetInt16();
                if (type == typeof(ushort))         return jsonElement.GetUInt16();
                if (type == typeof(int))            return jsonElement.GetInt32();
                if (type == typeof(uint))           return jsonElement.GetUInt32();
                if (type == typeof(long))           return jsonElement.GetInt64();
                if (type == typeof(ulong))          return jsonElement.GetUInt64();
                if (type == typeof(float))          return jsonElement.GetSingle();
                if (type == typeof(double))         return jsonElement.GetDouble();
                if (type == typeof(decimal))        return jsonElement.GetDecimal();
                if (type == typeof(BigInteger))     return BigInteger.Parse(jsonElement.GetRawText());
            }
            else if (jsonElement.ValueKind == JsonValueKind.String)
            {
                if (type == typeof(Cipher.SHA256))          return new Cipher.SHA256(Discreet.Common.Printable.Byteify(jsonElement.GetString()), false);
                if (type == typeof(Cipher.SHA512))          return new Cipher.SHA512(Discreet.Common.Printable.Byteify(jsonElement.GetString()), false);
                if (type == typeof(Cipher.Keccak))          return new Cipher.Keccak(Discreet.Common.Printable.Byteify(jsonElement.GetString()), false);
                if (type == typeof(Cipher.RIPEMD160))       return new Cipher.RIPEMD160(Discreet.Common.Printable.Byteify(jsonElement.GetString()), false);
                if (type == typeof(Cipher.Signature))       return new Cipher.Signature(Discreet.Common.Printable.Byteify(jsonElement.GetString()));
                if (type == typeof(Cipher.Key))             return new Cipher.Key(Discreet.Common.Printable.Byteify(jsonElement.GetString()));
                if (type == typeof(Coin.StealthAddress))    return new Coin.StealthAddress(jsonElement.GetString());
                if (type == typeof(Coin.TAddress))          return new Coin.TAddress(jsonElement.GetString());
                if (type == typeof(Coin.IAddress))          if (jsonElement.GetString().Length == 95) return new Coin.StealthAddress(jsonElement.GetString()); else return new Coin.TAddress(jsonElement.GetString());

                return jsonElement.GetString();
            }
            else if (jsonElement.ValueKind == JsonValueKind.True)
            {
                return true;
            }
            else if (jsonElement.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            return null;
        }

        public class StringConverter : JsonConverter<string>
        {
            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Number)
                {
                    var stringValue = reader.GetInt32();
                    return stringValue.ToString();
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
                    return reader.GetString();
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value);
            }

        }

        public static ulong DISToAmount(decimal amount)
        {
            return (ulong)Math.Floor(amount * 10000000000);
        }
    }
}
