using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Common;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Discreet.RPC.Converters
{
    public class BytesConverter : JsonConverter<byte[]>
    {
        public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Printable.Byteify(reader.GetString());

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
            => writer.WriteStringValue(Printable.Hexify(value));
    }
}
