using Discreet.Cipher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.Common.Converters
{
    public class SignatureConverter : JsonConverter<Signature>
    {
        public override Signature Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Signature.FromHex(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, Signature value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToHex());
    }
}
