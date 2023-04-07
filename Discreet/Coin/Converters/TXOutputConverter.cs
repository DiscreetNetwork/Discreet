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
    public class TXOutputConverter : JsonConverter<TXOutput>
    {
        public override TXOutput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            string innerPropName;
            TXOutput poutput = new();

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
                    case "TransactionSrc":
                        if (reader.TokenType == JsonTokenType.Null)
                            poutput.TransactionSrc = default;
                        else
                            poutput.TransactionSrc = SHA256.FromHex(reader.GetString());
                        break;
                    case "UXKey":
                        if (reader.TokenType == JsonTokenType.Null)
                            poutput.UXKey = default;
                        else
                            poutput.UXKey = Key.FromHex(reader.GetString());
                        break;
                    case "Commitment":
                        if (reader.TokenType == JsonTokenType.Null)
                            poutput.Commitment = default;
                        else
                            poutput.Commitment = Key.FromHex(reader.GetString());
                        break;
                    case "Amount":
                        poutput.Amount = reader.GetUInt64();
                        break;
                    default:
                        throw new JsonException();
                }
            }

            return poutput;
        }

        public override void Write(Utf8JsonWriter writer, TXOutput value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            if (value.TransactionSrc != default(SHA256) && value.TransactionSrc.Bytes != null)
            {
                writer.WritePropertyName(nameof(value.TransactionSrc));
                writer.WriteStringValue(value.TransactionSrc.ToHex());
            }
            
            writer.WritePropertyName(nameof(value.UXKey));
            if (value.UXKey == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.UXKey.ToHex());

            writer.WritePropertyName(nameof(value.Commitment));
            if (value.Commitment == default) writer.WriteNullValue();
            else writer.WriteStringValue(value.Commitment.ToHex());

            writer.WriteNumber(nameof(value.Amount), value.Amount);

            writer.WriteEndObject();
        }
    }
}
