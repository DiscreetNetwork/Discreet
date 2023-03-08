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
    public class SHA512Converter : JsonConverter<SHA512>
    {
        public override SHA512 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(Printable.Byteify(reader.GetString()!), false);

        public override void Write(Utf8JsonWriter writer, SHA512 value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToHex());
    }
}