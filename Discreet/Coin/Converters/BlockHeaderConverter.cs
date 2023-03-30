using Discreet.Cipher;
using Discreet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.Coin.Converters
{
    internal class BlockHeaderConverter : JsonConverter<BlockHeader>
    {
        public override BlockHeader Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            BlockHeader blockHeader = new BlockHeader();
            string? propName;
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                propName = reader.GetString();
                reader.Read();

                switch (propName)
                {
                    case "Version":
                        blockHeader.Version = reader.GetByte();
                        break;
                    case "Timestamp":
                        blockHeader.Timestamp = reader.GetUInt64();
                        break;
                    case "Height":
                        blockHeader.Height = reader.GetInt64();
                        break;
                    case "Fee":
                        blockHeader.Fee = reader.GetUInt64();
                        break;
                    case "PreviousBlock":
                        blockHeader.PreviousBlock = SHA256.FromHex(reader.GetString());
                        break;
                    case "BlockHash":
                        blockHeader.BlockHash = SHA256.FromHex(reader.GetString());
                        break;
                    case "MerkleRoot":
                        blockHeader.MerkleRoot = SHA256.FromHex(reader.GetString());
                        break;
                    case "NumTXs":
                        blockHeader.NumTXs = reader.GetUInt32();
                        break;
                    case "BlockSize":
                        blockHeader.BlockSize = reader.GetUInt32();
                        break;
                    case "NumOutputs":
                        blockHeader.NumOutputs = reader.GetUInt32();
                        break;
                    case "ExtraLen":
                        blockHeader.ExtraLen = reader.GetUInt32();
                        break;
                    case "Extra":
                        blockHeader.Extra = Printable.Byteify(reader.GetString());
                        break;
                    default:
                        throw new JsonException();
                }
            }

            return blockHeader;
        }

        public override void Write(Utf8JsonWriter writer, BlockHeader value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WriteNumber(nameof(value.Version), value.Version);
            writer.WriteNumber(nameof(value.Timestamp), value.Timestamp);
            writer.WriteNumber(nameof(value.Height), value.Height);
            writer.WriteNumber(nameof(value.Fee), value.Fee);

            writer.WriteString(nameof(value.PreviousBlock), value.PreviousBlock.ToHex());
            writer.WriteString(nameof(value.BlockHash), value.BlockHash.ToHex());
            writer.WriteString(nameof(value.MerkleRoot), value.MerkleRoot.ToHex());

            writer.WriteNumber(nameof(value.NumTXs), value.NumTXs);
            writer.WriteNumber(nameof(value.BlockSize), value.BlockSize);
            writer.WriteNumber(nameof(value.NumOutputs), value.NumOutputs);

            writer.WriteNumber(nameof(value.ExtraLen), value.ExtraLen);
            writer.WriteString(nameof(value.Extra), Printable.Hexify(value.Extra));

            writer.WriteEndObject();
        }
    }
}
