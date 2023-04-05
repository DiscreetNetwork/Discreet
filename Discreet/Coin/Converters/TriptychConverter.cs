using Discreet.Cipher;
using Discreet.Coin.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Discreet.Coin.Converters
{
    public class TriptychConverter : JsonConverter<Models.Triptych>
    {
        public override Models.Triptych Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            string innerPropName;
            Models.Triptych triptych = new();

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
                    case "K":
                        if (reader.TokenType == JsonTokenType.Null)
                            triptych.K = default;
                        else
                            triptych.K = Key.FromHex(reader.GetString());
                        break;
                    case "A":
                        if (reader.TokenType == JsonTokenType.Null)
                            triptych.A = default;
                        else
                            triptych.A = Key.FromHex(reader.GetString());
                        break;
                    
                    case "B":
                        if (reader.TokenType == JsonTokenType.Null)
                            triptych.B = default;
                        else
                            triptych.B = Key.FromHex(reader.GetString());
                        break;
                    case "C":
                        if (reader.TokenType == JsonTokenType.Null)
                            triptych.C = default;
                        else
                            triptych.C = Key.FromHex(reader.GetString());
                        break;
                    case "D":
                        if (reader.TokenType == JsonTokenType.Null)
                            triptych.D = default;
                        else
                            triptych.D = Key.FromHex(reader.GetString());
                        break;
                    case "X":
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            triptych.X = null;
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();

                        List<Key> xs = new();
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray) break;
                            xs.Add(Key.FromHex(reader.GetString()));
                        }
                        triptych.X = xs.ToArray();
                        break;
                    case "Y":
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            triptych.Y = null;
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();

                        List<Key> ys = new();
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray) break;
                            ys.Add(Key.FromHex(reader.GetString()));
                        }
                        triptych.Y = ys.ToArray();
                        break;
                    case "f":
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            triptych.f = null;
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();

                        List<Key> fs = new();
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray) break;
                            fs.Add(Key.FromHex(reader.GetString()));
                        }
                        triptych.f = fs.ToArray();
                        break;
                    case "zA":
                        if (reader.TokenType == JsonTokenType.Null)
                            triptych.zA = default;
                        else
                            triptych.zA = Key.FromHex(reader.GetString());
                        break;
                    case "zC":
                        if (reader.TokenType == JsonTokenType.Null)
                            triptych.zC = default;
                        else
                            triptych.zC = Key.FromHex(reader.GetString());
                        break;
                    case "z":
                        if (reader.TokenType == JsonTokenType.Null)
                            triptych.z = default;
                        else
                            triptych.z = Key.FromHex(reader.GetString());
                        break;
                    default:
                        throw new JsonException();
                }
            }

            return triptych;
        }

        public override void Write(Utf8JsonWriter writer, Models.Triptych value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName(nameof(value.K));
            if (value.K == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.K.ToHex());

            writer.WritePropertyName(nameof(value.A));
            if (value.A == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.A.ToHex());

            writer.WritePropertyName(nameof(value.B));
            if (value.B == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.B.ToHex());

            writer.WritePropertyName(nameof(value.C));
            if (value.C == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.C.ToHex());

            writer.WritePropertyName(nameof(value.D));
            if (value.D == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.D.ToHex());

            writer.WritePropertyName(nameof(value.X));
            if (value.X == null) writer.WriteNullValue();
            else
            {
                writer.WriteStartArray();
                for (int i = 0; i < value.X.Length; i++) writer.WriteStringValue(value.X[i].ToHex());
                writer.WriteEndArray();
            }

            writer.WritePropertyName(nameof(value.Y));
            if (value.Y == null) writer.WriteNullValue();
            else
            {
                writer.WriteStartArray();
                for (int i = 0; i < value.Y.Length; i++) writer.WriteStringValue(value.Y[i].ToHex());
                writer.WriteEndArray();
            }

            writer.WritePropertyName(nameof(value.f));
            if (value.f == null) writer.WriteNullValue();
            else
            {
                writer.WriteStartArray();
                for (int i = 0; i < value.f.Length; i++) writer.WriteStringValue(value.f[i].ToHex());
                writer.WriteEndArray();
            }

            writer.WritePropertyName(nameof(value.zA));
            if (value.zA == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.zA.ToHex());

            writer.WritePropertyName(nameof(value.zC));
            if (value.zC == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.zC.ToHex());

            writer.WritePropertyName(nameof(value.z));
            if (value.z == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.z.ToHex());

            writer.WriteEndObject();
        }
    }
}