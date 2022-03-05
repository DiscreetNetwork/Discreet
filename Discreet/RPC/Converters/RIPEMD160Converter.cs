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
    public class RIPEMD160Converter : JsonConverter<RIPEMD160>
    {
        public override RIPEMD160 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(Discreet.Common.Printable.Byteify(reader.GetString()!), false);

        public override void Write(Utf8JsonWriter writer, RIPEMD160 value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToHex());
    }
}
