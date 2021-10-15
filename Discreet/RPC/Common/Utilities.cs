using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Discreet.RPC.Common
{
    class Utilities
    {
        public class StringConverter : JsonConverter<string>
        {
            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {

                if (reader.TokenType == JsonTokenType.Number)
                {
                    var stringValue = reader.GetInt32();
                    return stringValue.ToString();
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
                    return reader.GetString();
                }

                throw new JsonException();
            }


            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value);
            }

        }

        public static ulong DISToAmount(decimal amount)
        {
            return (ulong)Math.Floor(amount * 10000000000);
        }
    }
}
