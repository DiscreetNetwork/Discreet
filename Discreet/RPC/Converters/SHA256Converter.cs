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
    public class SHA256Converter : JsonConverter<SHA256>
    {
        public override SHA256 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            SHA256.FromHex(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, SHA256 value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToHex());
    }
}
