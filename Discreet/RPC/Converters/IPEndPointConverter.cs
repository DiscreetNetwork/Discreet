using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Net;
using System.Text.Json;

namespace Discreet.RPC.Converters
{
    public class IPEndPointConverter : JsonConverter<IPEndPoint>
    {
        public override IPEndPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => IPEndPoint.Parse(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
    }
}
