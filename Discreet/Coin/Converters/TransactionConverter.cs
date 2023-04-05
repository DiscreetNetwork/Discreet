using Discreet.Cipher;
using Discreet.Coin.Models;
using Discreet.Common;
using Discreet.Common.Converters;
using Discreet.Readable;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.Coin.Converters
{
    public class TransactionConverter : JsonConverter<Models.FullTransaction>
    {
        private static BulletproofConverter BulletproofConverter;
        private static BulletproofPlusConverter BulletproofPlusConverter;
        private static SignatureConverter SignatureConverter;
        private static Transparent.TXInputConverter TTXInputConverter;
        private static Transparent.TXOutputConverter TTXOutputConverter;
        private static TriptychConverter TriptychConverter;
        private static TXInputConverter TXInputConverter;
        private static TXOutputConverter TXOutputConverter;
        private static KeyConverter KeyConverter;

        static TransactionConverter()
        {
            BulletproofConverter = new();
            BulletproofPlusConverter = new();
            SignatureConverter = new();
            TTXInputConverter = new();
            TTXOutputConverter = new();
            TriptychConverter = new();
            TXInputConverter = new();
            TXOutputConverter = new();
            KeyConverter = new();
        }

        private static T[] ReadArray<T>(ref Utf8JsonReader reader, JsonConverter<T> converter, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartArray) throw new Exception();
            var tinputs = new List<T>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;
                tinputs.Add(converter.Read(ref reader, typeof(T), options));
            }
            return tinputs.ToArray();
        }


        public override Models.FullTransaction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            Models.FullTransaction transaction = new();
            string? propName;
            byte version;

            // read header first
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndObject) return null;
            propName = reader.GetString();
            if (propName != "Version") throw new JsonException("FullTransaction expects the first field to be Version");

            reader.Read();
            version = reader.GetByte();
            if (version >= (byte)Config.TransactionVersions.END) throw new JsonException("FullTransaction version exceeds defined range");

            switch (version)
            {
                case 0 or 1 or 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                default:
                    throw new Exception("unreachable");
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return transaction;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    propName = reader.GetString();
                    reader.Read();
                    switch (propName)
                    {
                        /* standard header */
                        case "Version":
                            throw new Exception("FullTransaction already received this field");
                        case "NumInputs":
                            transaction.NumInputs = reader.GetByte();
                            break;
                        case "NumOutputs":
                            transaction.NumOutputs = reader.GetByte();
                            break;
                        case "NumSigs":
                            transaction.NumSigs = reader.GetByte();
                            break;
                        /* mixedtx header */
                        case "NumTInputs":
                            transaction.NumTInputs = reader.GetByte();
                            break;
                        case "NumPInputs":
                            transaction.NumPInputs = reader.GetByte();
                            break;
                        case "NumTOutputs":
                            transaction.NumTOutputs = reader.GetByte();
                            break;
                        case "NumPOutputs":
                            transaction.NumPOutputs = reader.GetByte();
                            break;

                        /* data fields */
                        case "Fee":
                            transaction.Fee = reader.GetUInt64();
                            break;
                        case "SigningHash" or "InnerHash":
                            transaction.SigningHash = SHA256.FromHex(reader.GetString());
                            break;
                        case "TInputs":
                            transaction.TInputs = ReadArray(ref reader, TTXInputConverter, options);
                            break;
                        case "TOutputs":
                            transaction.TOutputs = ReadArray(ref reader, TTXOutputConverter, options);
                            break;
                        case "TSignatures":
                            transaction.TSignatures = ReadArray(ref reader, SignatureConverter, options);
                            break;
                        case "TransactionKey":
                            var tkstr = reader.GetString();
                            if (tkstr.Length is not 64) throw new Exception("Expected data to be of length 64");
                            transaction.TransactionKey = Key.FromHex(tkstr);
                            break;
                        case "PInputs":
                            transaction.PInputs = ReadArray(ref reader, TXInputConverter, options);
                            break;
                        case "POutputs":
                            transaction.POutputs = ReadArray(ref reader, TXOutputConverter, options);
                            break;
                        case "RangeProof":
                            transaction.RangeProof = BulletproofConverter.Read(ref reader, typeof(Models.Bulletproof), options);
                            break;
                        case "RangeProofPlus":
                            transaction.RangeProofPlus = BulletproofPlusConverter.Read(ref reader, typeof(Models.BulletproofPlus), options);
                            break;
                        case "PSignatures":
                            transaction.PSignatures = ReadArray(ref reader, TriptychConverter, options);
                            break;
                        case "PseudoOutputs":
                            transaction.PseudoOutputs = ReadArray(ref reader, KeyConverter, options)?.Select(x => x.Value).ToArray();
                            break;
                        case "Inputs":
                            switch (transaction.Version)
                            {
                                case 0 or 1 or 2:
                                    transaction.PInputs = ReadArray(ref reader, TXInputConverter, options);
                                    break;
                                case 3:
                                    transaction.TInputs = ReadArray(ref reader, TTXInputConverter, options);
                                    break;
                                default:
                                    throw new Exception();
                            }
                            break;
                        case "Outputs":
                            switch (transaction.Version)
                            {
                                case 0 or 1 or 2:
                                    transaction.POutputs = ReadArray(ref reader, TXOutputConverter, options);
                                    break;
                                case 3:
                                    transaction.TOutputs = ReadArray(ref reader, TTXOutputConverter, options);
                                    break;
                                default:
                                    throw new Exception();
                            }
                            break;
                        case "Signatures":
                            switch (transaction.Version)
                            {
                                case 0 or 1 or 2:
                                    transaction.PSignatures = ReadArray(ref reader, TriptychConverter, options);
                                    break;
                                case 3:
                                    transaction.TSignatures = ReadArray(ref reader, SignatureConverter, options);
                                    break;
                                default:
                                    throw new Exception();
                            }
                            break;
                        case "TxID":
                            // this is recalculated as needed.
                            break;
                        default:
                            throw new Exception();
                    }
                }
            }

            return transaction;
        }

        private static void WriteArray<T>(Utf8JsonWriter writer, T[] values, JsonConverter<T> converter, JsonSerializerOptions options)
        {
            if (values == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();
            for (int i = 0; i < values.Length; i++)
            {
                converter.Write(writer, values[i], options);
            }
            writer.WriteEndArray();
        }

        public override void Write(Utf8JsonWriter writer, Models.FullTransaction value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WriteNumber(nameof(value.Version), value.Version);
            writer.WriteNumber(nameof(value.NumInputs), value.NumInputs);
            writer.WriteNumber(nameof(value.NumOutputs), value.NumOutputs);
            writer.WriteNumber(nameof(value.NumSigs), value.NumSigs);

            if (value.Version == 4)
            {
                writer.WriteNumber(nameof(value.NumTInputs), value.NumTInputs);
                writer.WriteNumber(nameof(value.NumPInputs), value.NumPInputs);
                writer.WriteNumber(nameof(value.NumTOutputs), value.NumTOutputs);
                writer.WriteNumber(nameof(value.NumPOutputs), value.NumPOutputs);
            }

            writer.WriteNumber(nameof(value.Fee), value.Fee);

            if (value.Version == 3 || value.Version == 4)
            {
                writer.WriteString(value.Version == 4 ? nameof(value.SigningHash) : "InnerHash", value.SigningHash.ToHex());
            }
            else
            {
                writer.WriteString(nameof(value.TransactionKey), value.TransactionKey.ToHex());
            }

            if (value.Version == 4 || value.Version == 3)
            {
                // write fields as individual elements
                if (value.TInputs != null && value.TInputs.Length > 0)
                {
                    writer.WritePropertyName(value.Version == 3 ? "Inputs" : nameof(value.TInputs));
                    WriteArray(writer, value.TInputs, TTXInputConverter, options);
                }

                if (value.TOutputs != null && value.TOutputs.Length > 0)
                {
                    writer.WritePropertyName(value.Version == 3 ? "Outputs" : nameof(value.TOutputs));
                    WriteArray(writer, value.TOutputs, TTXOutputConverter, options);
                }

                if (value.TSignatures != null && value.TSignatures.Length > 0)
                {
                    writer.WritePropertyName(value.Version == 3 ? "Signatures" : nameof(value.TSignatures));
                    WriteArray(writer, value.TSignatures, SignatureConverter, options);
                }

                if (value.TransactionKey != default)
                {
                    writer.WriteString(nameof(value.TransactionKey), value.TransactionKey.ToHex());
                }
            }

            if (value.Version != 3 && value.Version < 5)
            {
                if (value.PInputs != null && value.PInputs.Length > 0)
                {
                    writer.WritePropertyName(value.Version != 4 ? "Inputs" : nameof(value.PInputs));
                    WriteArray(writer, value.PInputs, TXInputConverter, options);
                }

                if (value.POutputs != null && value.POutputs.Length > 0)
                {
                    writer.WritePropertyName(value.Version != 4 ? "Outputs" : nameof(value.POutputs));
                    WriteArray(writer, value.POutputs, TXOutputConverter, options);
                }

                if (value.RangeProof != null)
                {
                    writer.WritePropertyName(nameof(value.RangeProof));
                    BulletproofConverter.Write(writer, value.RangeProof, options);
                }
                else if (value.RangeProofPlus != null)
                {
                    writer.WritePropertyName(nameof(value.RangeProofPlus));
                    BulletproofPlusConverter.Write(writer, value.RangeProofPlus, options);
                }

                if (value.PSignatures != null && value.PSignatures.Length > 0)
                {
                    writer.WritePropertyName(value.Version != 4 ? "Signatures" : nameof(value.PSignatures));
                    WriteArray(writer, value.PSignatures, TriptychConverter, options);
                }

                if (value.PseudoOutputs != null && value.PseudoOutputs.Length > 0)
                {
                    writer.WritePropertyName(nameof(value.PseudoOutputs));
                    WriteArray(writer, value.PseudoOutputs.Select(x => (Key?)x).ToArray(), KeyConverter, options);
                }
            }

            writer.WriteString(nameof(value.TxID), value.TxID.ToHex());

            writer.WriteEndObject();
        }
    }
}
