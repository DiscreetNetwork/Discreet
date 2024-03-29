﻿using Discreet.Cipher;
using Discreet.Coin.Models;
using Discreet.Common;
using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.Coin.Converters
{
    public class TTXInputConverter : JsonConverter<TTXInput>
    {
        public override TTXInput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            TTXInput tinput = new();
            byte[] data = Printable.Byteify(reader.GetString());
            if (data.Length is not 33) throw new Exception("Expected data to be of length 33");
            tinput.TxSrc = new SHA256(data, 0);
            tinput.Offset = data[32];

            return tinput;
        }

        public override void Write(Utf8JsonWriter writer, TTXInput value, JsonSerializerOptions options)
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
