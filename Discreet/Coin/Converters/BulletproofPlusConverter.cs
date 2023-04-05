using Discreet.Cipher;
using Discreet.Coin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.Coin.Converters
{
    public class BulletproofPlusConverter : JsonConverter<Models.BulletproofPlus>
    {
        public override Models.BulletproofPlus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            string innerPropName;
            Models.BulletproofPlus bp = new();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                innerPropName = reader.GetString();
                reader.Read();
                switch (innerPropName)
                {
                    case "size":
                        bp.size = reader.GetUInt32();
                        break;
                    case "A":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.A = default;
                        else
                            bp.A = Key.FromHex(reader.GetString());
                        break;
                    case "A1":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.A1 = default;
                        else
                            bp.A1 = Key.FromHex(reader.GetString());
                        break;
                    case "B":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.B = default;
                        else
                            bp.B = Key.FromHex(reader.GetString());
                        break;
                    case "r1":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.r1 = default;
                        else
                            bp.r1 = Key.FromHex(reader.GetString());
                        break;
                    case "s1":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.s1 = default;
                        else
                            bp.s1 = Key.FromHex(reader.GetString());
                        break;
                    case "d1":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.d1 = default;
                        else
                            bp.d1 = Key.FromHex(reader.GetString());
                        break;
                    case "L":
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            bp.L = null;
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();

                        List<Key> ls = new();
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray) break;
                            ls.Add(Key.FromHex(reader.GetString()));
                        }
                        bp.L = ls.ToArray();
                        break;
                    case "R":
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            bp.R = null;
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();

                        List<Key> rs = new();
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray) break;
                            rs.Add(Key.FromHex(reader.GetString()));
                        }
                        bp.R = rs.ToArray();
                        break;
                    default:
                        throw new JsonException();
                }
            }

            return bp;
        }

        public override void Write(Utf8JsonWriter writer, Models.BulletproofPlus value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WriteNumber(nameof(value.size), value.size);

            writer.WritePropertyName(nameof(value.A));
            if (value.A == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.A.ToHex());

            writer.WritePropertyName(nameof(value.A1));
            if (value.A1 == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.A1.ToHex());

            writer.WritePropertyName(nameof(value.B));
            if (value.B == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.B.ToHex());

            writer.WritePropertyName(nameof(value.r1));
            if (value.r1 == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.r1.ToHex());

            writer.WritePropertyName(nameof(value.s1));
            if (value.s1 == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.s1.ToHex());

            writer.WritePropertyName(nameof(value.d1));
            if (value.d1 == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.d1.ToHex());

            writer.WritePropertyName(nameof(value.L));
            if (value.L == null) writer.WriteNullValue();
            else
            {
                writer.WriteStartArray();
                for (int i = 0; i < value.L.Length; i++) writer.WriteStringValue(value.L[i].ToHex());
                writer.WriteEndArray();
            }

            writer.WritePropertyName(nameof(value.R));
            if (value.R == null) writer.WriteNullValue();
            else
            {
                writer.WriteStartArray();
                for (int i = 0; i < value.R.Length; i++) writer.WriteStringValue(value.R[i].ToHex());
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }
}