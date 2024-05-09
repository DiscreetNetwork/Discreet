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
    public class ChainScriptConverter : JsonConverter<ChainScript>
    {
        public override ChainScript Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            string innerPropName;
            ChainScript script = new();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                innerPropName = reader.GetString();
                switch (innerPropName)
                {
                    case "Version":
                        script.Version = reader.GetUInt32();
                        break;
                    case "Code":
                        script.Code = Common.Printable.Byteify(reader.GetString()); break;
                    case "Data":
                        script.Data = Common.Printable.Byteify(reader.GetString()); break;
                    default: throw new JsonException();
                }
            }

            return script;
        }

        public override void Write(Utf8JsonWriter writer, ChainScript value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WriteNumber("Version", value.Version);
            writer.WriteString("Code", Common.Printable.Hexify(value.Code ?? Array.Empty<byte>()));
            writer.WriteString("Data", Common.Printable.Hexify(value.Data ?? Array.Empty<byte>()));
            writer.WriteEndObject();
        }
    }
}
