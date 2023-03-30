using Discreet.Cipher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.Coin.Converters
{
    public class TXInputConverter : JsonConverter<TXInput>
    {
        public override TXInput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            string innerPropName;
            TXInput pinput = new();

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
                    case "Offsets":
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            pinput.Offsets = null;
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.StartArray) throw new Exception();
                        List<uint> poffsets = new();
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray) break;
                            poffsets.Add(reader.GetUInt32());
                        }
                        pinput.Offsets = poffsets.ToArray();
                        break;
                    case "KeyImage":
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            pinput.KeyImage = default;
                            break;
                        }
                        pinput.KeyImage = Key.FromHex(reader.GetString());
                        break;
                    default:
                        throw new JsonException();
                }
            }

            return pinput;
        }

        public override void Write(Utf8JsonWriter writer, TXInput value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName(nameof(value.Offsets));
            if (value.Offsets != null)
            {
                writer.WriteStartArray();

                for (int i = 0; i < value.Offsets.Length; i++)
                {
                    writer.WriteNumberValue(value.Offsets[i]);
                }

                writer.WriteEndArray();
            }
            else
            {
                writer.WriteNull(nameof(value.Offsets));
            }

            writer.WritePropertyName(nameof(value.KeyImage));
            if (value.KeyImage == default)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.KeyImage.ToHex());
            }

            writer.WriteEndObject();
        }
    }
}
