using Discreet.Cipher;
using Discreet.Coin.Models;
using Discreet.Coin.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.Coin.Converters
{
    public class ScriptTXOutputConverter : JsonConverter<ScriptTXOutput>
    {
        private static DatumConverter _datumConverter;
        private static ChainScriptConverter _chainScriptConverter;

        static ScriptTXOutputConverter()
        {
            _chainScriptConverter = new ChainScriptConverter();
            _datumConverter = new DatumConverter();
        }

        public override ScriptTXOutput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            string innerPropName;
            ScriptTXOutput toutput = new();

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
                    case "Datum":
                        toutput.Datum = _datumConverter.Read(ref reader, typeof(Datum), options);
                        break;
                    case "DatumHash":
                        toutput.DatumHash = SHA256.FromHex(reader.GetString());
                        break;
                    case "ReferenceScript":
                        toutput.ReferenceScript = _chainScriptConverter.Read(ref reader, typeof(ChainScript), options);
                        break;
                    default:
                        throw new JsonException();
                }
            }

            return toutput;
        }

        public override void Write(Utf8JsonWriter writer, ScriptTXOutput value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            if (value.TransactionSrc != default && value.TransactionSrc.Bytes != null)
            {
                writer.WriteString(nameof(value.TransactionSrc), value.TransactionSrc.ToHex());
            }

            if (value.Address != null)
            {
                writer.WriteString(nameof(value.Address), value.Address.ToString());
            }

            writer.WriteNumber(nameof(value.Amount), value.Amount);

            if (value.Datum != null)
            {
                writer.WritePropertyName(nameof(value.Datum));
                _datumConverter.Write(writer, value.Datum, options);
            }

            if (value.DatumHash != default && value.DatumHash.Value != default && value.DatumHash.Value.Bytes != null)
            {
                writer.WriteString(nameof(value.DatumHash), value.DatumHash.Value.ToHex());
            }

            if (value.ReferenceScript != null && value.ReferenceScript.Version > 0)
            {
                writer.WritePropertyName(nameof(value.ReferenceScript));
                _chainScriptConverter.Write(writer, value.ReferenceScript, options);
            }

            writer.WriteEndObject();
        }
    }
}
