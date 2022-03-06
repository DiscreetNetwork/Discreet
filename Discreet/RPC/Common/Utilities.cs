using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Collections;

namespace Discreet.RPC.Common
{
    public class Utilities
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
    }
}
