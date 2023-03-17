using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.Wallets.Utilities.Converters
{
    public class SQLiteWalletConverter : JsonConverter<SQLiteWallet>
    {
        public override SQLiteWallet? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
            Discreet.Wallets.SQLiteWallet wallet = new();
            string? propName;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return wallet;
;
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    propName = reader.GetString();
                    reader.Read();
                    switch (propName)
                    {
                        case "Path":
                            wallet.Path = reader.GetString();
                            break;
                        case "Label":
                            wallet.Label = reader.GetString();
                            break;
                        case "CoinName":
                            break;
                        case "Timestamp":
                            wallet.Timestamp = (ulong)reader.GetDateTime().Ticks;
                            break;
                        case "Version":
                            break;
                        case "LastSeenHeight":
                            wallet.SetLastSeenHeight(reader.GetInt64());
                            break;
                        case "Encrypted":
                            wallet.Encrypted = reader.GetBoolean();
                            break;
                        case "Locked":
                            wallet.IsEncrypted = reader.GetBoolean();
                            break;
                        case "EntropyChecksum":
                            wallet.EntropyChecksum = new Cipher.SHA256(Common.Printable.Byteify(reader.GetString()), false);
                            break;
                        case "Accounts":
                            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonTokenType.EndArray) break;
                                var addr = reader.GetString();
                                wallet.Accounts.Add(new Discreet.Wallets.Models.Account { Address = addr });
                            }
                            throw new JsonException();
                        default:
                            throw new JsonException();
                    }
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Discreet.Wallets.SQLiteWallet value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Path", value.Path);
            writer.WriteString("Label", value.Label);
            writer.WriteString("CoinName", value.CoinName);
            writer.WriteString("Version", value.Version);
            writer.WriteBoolean("Encrypted", value.Encrypted);
            writer.WriteBoolean("Locked", value.IsEncrypted);
            writer.WriteString("EntropyChecksum", value.EntropyChecksum.ToHex());
            writer.WriteNumber("Timestamp", value.Timestamp);
            writer.WriteNumber("LastSeenHeight", value.GetLastSeenHeight());
            writer.WritePropertyName("Accounts");
            writer.WriteStartArray();
            value.Accounts.ForEach(acc => writer.WriteStringValue(acc.Address));
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
