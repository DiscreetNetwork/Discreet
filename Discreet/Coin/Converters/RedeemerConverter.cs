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
    public class RedeemerConverter : JsonConverter<(byte, Datum)>
    {
        private static DatumConverter DatumConverter;

        static RedeemerConverter()
        {
            DatumConverter = new DatumConverter();
        }

        public override (byte, Datum) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return ((byte)255, null);

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            string innerPropName;
            byte? _index = null;
            Datum _datum = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                innerPropName = reader.GetString();
                switch (innerPropName)
                {
                    case "Index":
                        _index = reader.GetByte();
                        break;
                    case "Datum":
                        _datum = DatumConverter.Read(ref reader, typeof(Datum), options);
                        break;
                    default: throw new JsonException();
                }
            }

            if (_index == null)
            {
                throw new Exception("missing one or both of: \"Index\", \"Datum\" in Redeemer");
            }

            return (_index.Value, _datum ?? Datum.Default());
        }

        public override void Write(Utf8JsonWriter writer, (byte, Datum) value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Index", value.Item1);
            writer.WritePropertyName("Datum");
            DatumConverter.Write(writer, value.Item2, options);
            writer.WriteEndObject();
        }
    }
}
