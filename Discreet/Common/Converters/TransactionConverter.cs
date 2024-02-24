using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Common;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Discreet.Common.Converters
{
    /*public class TransactionConverter : JsonConverter<Readable.FullTransaction>
    {
        public override Readable.FullTransaction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            reader.Read();

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            string? propertyName = reader.GetString();

            if (propertyName != "Version" && propertyName != "version")
            {
                throw new JsonException();
            }

            reader.Read();

            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }

            byte _version = reader.GetByte();

            return _version switch
            {
                0 or 1 or 2 => Private(ref reader, typeToConvert, options).ToFull(),
                3 => Transparent(ref reader, typeToConvert, options).ToFull(),
                4 => Mixed(ref reader, typeToConvert, options).ToFull(),
                _ => Full(ref reader, typeToConvert, options)
            };
        }

        private Readable.Transaction Private(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {

        }

        private Readable.Transparent.Transaction Transparent(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {

        }

        private Readable.MixedTransaction Mixed(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {

        }

        private Readable.FullTransaction Full(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {

        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
            => writer.WriteStringValue(Printable.Hexify(value));
    }*/
}
