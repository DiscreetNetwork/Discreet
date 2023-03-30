using Discreet.Cipher;
using Discreet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.Coin.Converters.Transparent
{
    public class TXInputConverter : JsonConverter<Coin.Transparent.TXInput>
    {
        public override Coin.Transparent.TXInput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            Coin.Transparent.TXInput tinput = new();
            byte[] data = Printable.Byteify(reader.GetString());
            if (data.Length is not 33) throw new Exception("Expected data to be of length 33");
            tinput.TxSrc = new SHA256(data, 0);
            tinput.Offset = data[32];

            return tinput;
        }

        public override void Write(Utf8JsonWriter writer, Coin.Transparent.TXInput value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(Printable.Hexify(value.Serialize()));
        }
    }
}
