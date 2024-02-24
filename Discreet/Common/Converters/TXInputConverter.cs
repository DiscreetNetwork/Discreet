using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discreet.Coin.Models;

namespace Discreet.Common.Converters
{
    public class TXInputConverter : JsonConverter<TTXInput>
    {
        public override TTXInput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(Printable.Byteify(reader.GetString()!));

        public override void Write(Utf8JsonWriter writer, TTXInput value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToReadable());
    }
}
