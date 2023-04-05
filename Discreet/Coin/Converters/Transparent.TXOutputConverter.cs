using Discreet.Cipher;
using Discreet.Coin.Models;
using Discreet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.Coin.Converters.Transparent
{
    public class TXOutputConverter : JsonConverter<TTXOutput>
    {
        public override TTXOutput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            string innerPropName;
            TTXOutput toutput = new();

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
                            toutput.TransactionSrc = default;
                        else
                            toutput.TransactionSrc = SHA256.FromHex(reader.GetString());
                        break;
                    case "Address":
                        if (reader.TokenType == JsonTokenType.Null)
                            toutput.Address = null;
                        else
                            toutput.Address = new TAddress(reader.GetString());
                        break;
                    case "Amount":
                        toutput.Amount = reader.GetUInt64();
                        break;
                    default:
                        throw new JsonException();
                }
            }

            return toutput;
        }

        public override void Write(Utf8JsonWriter writer, TTXOutput value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            if (value.TransactionSrc != default)
            {
                writer.WriteString(nameof(value.TransactionSrc), value.TransactionSrc.ToHex());
            }

            if (value.Address != null)
            {
                writer.WriteString(nameof(value.Address), value.TransactionSrc.ToHex());
            }

            writer.WriteNumber(nameof(value.Amount), value.Amount);

            writer.WriteEndObject();
        }
    }
}