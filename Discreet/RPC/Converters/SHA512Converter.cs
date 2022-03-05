using Discreet.Cipher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.RPC.Converters
{
    public class SHA512Converter : JsonConverter<SHA512>
    {
        public override SHA512 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(Discreet.Common.Printable.Byteify(reader.GetString()!), false);

        public override void Write(Utf8JsonWriter writer, SHA512 value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToHex());
    }
}