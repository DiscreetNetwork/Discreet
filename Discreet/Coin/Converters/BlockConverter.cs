using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discreet.Coin.Models;

namespace Discreet.Coin.Converters
{
    public class BlockConverter : JsonConverter<Block>
    {
        private static BlockHeaderConverter BlockHeaderConverter;
        private static TransactionConverter TransactionConverter;

        static BlockConverter()
        {
            BlockHeaderConverter = new();
            TransactionConverter = new();
        }

        public override Block Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            Block block = new Block();
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
                    case "Header":
                        block.Header = BlockHeaderConverter.Read(ref reader, typeof(BlockHeader), options);
                        break;
                    case "Transactions":
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            block.Transactions = null;
                            break;
                        }
                        if (reader.TokenType != JsonTokenType.StartArray) throw new Exception();
                        var txs = new List<FullTransaction>();
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray) break;
                            txs.Add(TransactionConverter.Read(ref reader, typeof(FullTransaction), options));
                        }
                        block.Transactions = txs.ToArray();
                        break;
                    default:
                        throw new JsonException();
                }
            }

            return block;
        }

        public override void Write(Utf8JsonWriter writer, Block value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName(nameof(value.Header));
            BlockHeaderConverter.Write(writer, value.Header, options);

            if (value.Transactions == null)
            {
                writer.WriteNull(nameof(value.Transactions));
            }
            else
            {
                writer.WritePropertyName(nameof(value.Transactions));
                writer.WriteStartArray();
                for (int i = 0; i < value.Transactions.Length; i++)
                {
                    TransactionConverter.Write(writer, value.Transactions[i], options);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }
}
