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
    public class BulletproofConverter : JsonConverter<Models.Bulletproof>
    {
        public override Models.Bulletproof Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            string innerPropName;
            Models.Bulletproof bp = new();

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
                    case "S":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.S = default;
                        else
                            bp.S = Key.FromHex(reader.GetString());
                        break;
                    case "T1":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.T1 = default;
                        else
                            bp.T1 = Key.FromHex(reader.GetString());
                        break;
                    case "T2":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.T2 = default;
                        else
                            bp.T2 = Key.FromHex(reader.GetString());
                        break;
                    case "taux":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.taux = default;
                        else
                            bp.taux = Key.FromHex(reader.GetString());
                        break;
                    case "mu":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.mu = default;
                        else
                            bp.mu = Key.FromHex(reader.GetString());
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
                    case "a":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.a = default;
                        else
                            bp.a = Key.FromHex(reader.GetString());
                        break;
                    case "b":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.b = default;
                        else
                            bp.b = Key.FromHex(reader.GetString());
                        break;
                    case "t":
                        if (reader.TokenType == JsonTokenType.Null)
                            bp.t = default;
                        else
                            bp.t = Key.FromHex(reader.GetString());
                        break;
                    default:
                        throw new JsonException();
                }
            }

            return bp;
        }

        public override void Write(Utf8JsonWriter writer, Models.Bulletproof value, JsonSerializerOptions options)
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

            writer.WritePropertyName(nameof(value.S));
            if (value.S == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.S.ToHex());

            writer.WritePropertyName(nameof(value.T1));
            if (value.T1 == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.T1.ToHex());

            writer.WritePropertyName(nameof(value.T2));
            if (value.T2 == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.T2.ToHex());

            writer.WritePropertyName(nameof(value.taux));
            if (value.taux == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.taux.ToHex());

            writer.WritePropertyName(nameof(value.mu));
            if (value.mu == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.mu.ToHex());

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

            writer.WritePropertyName(nameof(value.a));
            if (value.a == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.a.ToHex());

            writer.WritePropertyName(nameof(value.b));
            if (value.b == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.b.ToHex());

            writer.WritePropertyName(nameof(value.t));
            if (value.t == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.t.ToHex());

            writer.WriteEndObject();
        }
    }
}
