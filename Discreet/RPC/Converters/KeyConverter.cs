using Discreet.Cipher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.RPC.Common
{
    public class KeyConverter : JsonConverter<Cipher.Key>
    {
        public override Key Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Key.FromHex(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, Key value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToHex());
    }
}
