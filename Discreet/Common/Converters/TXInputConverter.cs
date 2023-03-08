using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.Common.Converters
{
    public class TXInputConverter : JsonConverter<Coin.Transparent.TXInput>
    {
        public override Coin.Transparent.TXInput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(Printable.Byteify(reader.GetString()!));

        public override void Write(Utf8JsonWriter writer, Coin.Transparent.TXInput value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToReadable());
    }
}
