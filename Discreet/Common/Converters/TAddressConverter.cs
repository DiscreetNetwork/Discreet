using Discreet.Coin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.Common.Converters
{
    public class TAddressConverter : JsonConverter<TAddress>
    {
        public override TAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, TAddress value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }
}
