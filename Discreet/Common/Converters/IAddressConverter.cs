using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Discreet.Coin;
using Discreet.Common;
using System.Text.Json;

namespace Discreet.Common.Converters
{
    public class IAddressConverter : JsonConverter<IAddress>
    {
        public override IAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string str = reader.GetString();

            if (str == null) throw new Exception("null encountered in IAddressConverter");

            if (str.Length == 95) return new StealthAddress(str);

            return new TAddress(str);
        }

        public override void Write(Utf8JsonWriter writer, IAddress value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }
}
