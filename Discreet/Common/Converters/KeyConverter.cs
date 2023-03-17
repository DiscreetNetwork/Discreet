using Discreet.Cipher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.Common.Converters
{
    public class KeyConverter : JsonConverter<Key?>
    {
        public override Key? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            else return Key.FromHex(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, Key? value, JsonSerializerOptions options)
        {
            if (value == null) writer.WriteNullValue();
            else if (value.Value.bytes == null) writer.WriteNullValue();
            else writer.WriteStringValue(value?.ToHex());
        }
    }
}
