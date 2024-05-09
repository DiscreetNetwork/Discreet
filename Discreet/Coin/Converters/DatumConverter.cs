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
    public class DatumConverter : JsonConverter<Datum>
    {
        public override Datum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            string innerPropName;
            Datum datum = new();

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
                        datum.Version = reader.GetByte();
                        break;
                    case "Data":
                        datum.Data = Common.Printable.Byteify(reader.GetString()); break;
                    default: throw new JsonException();
                }
            }

            return datum;
        }

        public override void Write(Utf8JsonWriter writer, Datum value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WriteNumber("Version", value.Version);
            writer.WriteString("Data", Common.Printable.Hexify(value.Data ?? Array.Empty<byte>()));
            writer.WriteEndObject();
        }
    }
}
