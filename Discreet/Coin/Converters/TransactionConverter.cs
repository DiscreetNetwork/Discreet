using Discreet.Cipher;
using Discreet.Coin.Models;
using Discreet.Coin.Script;
using Discreet.Common;
using Discreet.Common.Converters;
using Discreet.Common.Serialize;
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
        private static TTXInputConverter TTXInputConverter;
        private static TTXOutputConverter TTXOutputConverter;
        private static TriptychConverter TriptychConverter;
        private static TXInputConverter TXInputConverter;
        private static TXOutputConverter TXOutputConverter;
        private static KeyConverter KeyConverter;
        private static DatumConverter DatumConverter;
        private static ChainScriptConverter ChainScriptConverter;
        private static ScriptTXOutputConverter ScriptTXOutputConverter;
        private static RedeemerConverter RedeemerConverter;
        private static StringConverter StringConverter;

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
            DatumConverter = new();
            ChainScriptConverter = new();
            ScriptTXOutputConverter = new();
            RedeemerConverter = new();
            StringConverter = new();
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

        private delegate T ElementReader<T>(ref Utf8JsonReader reader, JsonSerializerOptions options);

        private static T[] ReadArray<T>(ref Utf8JsonReader reader, ElementReader<T> converter, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartArray) throw new Exception();
            var tinputs = new List<T>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;
                tinputs.Add(converter(ref reader, options));
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
            transaction.Version = version;

            switch (version)
            {
                case 0 or 1 or 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                default:
                    throw new Exception("unreachable");
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;

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
                        case "NumRefInputs":
                            transaction.NumRefInputs = reader.GetByte();
                            break;
                        case "NumScriptInputs":
                            transaction.NumScriptInputs = reader.GetByte();
                            break;

                        /* data fields */
                        case "Fee":
                            transaction.Fee = reader.GetUInt64();
                            break;
                        case "SigningHash" or "InnerHash":
                            transaction.SigningHash = SHA256.FromHex(reader.GetString());
                            break;
                        case "ValidityInterval":
                            // interval encoded as array
                            {
                                if (reader.TokenType == JsonTokenType.Null) transaction.ValidityInterval = (-1, long.MaxValue);
                                if (reader.TokenType != JsonTokenType.StartArray) throw new Exception();
                                reader.Read();

                                if (reader.TokenType == JsonTokenType.EndArray) throw new Exception();
                                var _validityIntervalLow = reader.GetInt64();
                                reader.Read();

                                if (reader.TokenType == JsonTokenType.EndArray) throw new Exception();
                                var _validityIntervalHigh = reader.GetInt64();
                                reader.Read();

                                if (reader.TokenType != JsonTokenType.EndArray) throw new Exception();
                                transaction.ValidityInterval = (_validityIntervalLow, _validityIntervalHigh);
                            }
                            break;
                        case "TInputs":
                            transaction.TInputs = ReadArray(ref reader, TTXInputConverter, options);
                            break;
                        case "RefInputs":
                            transaction.RefInputs = ReadArray(ref reader, TTXInputConverter, options);
                            break;
                        case "TOutputs":
                            transaction.TOutputs = ReadArray(ref reader, ScriptTXOutputConverter, options);
                            break;
                        case "TSignatures":
                            transaction.TSignatures = ReadArray<(byte, Signature)>(ref reader, (ref Utf8JsonReader _reader, JsonSerializerOptions _options) =>
                            {
                                var _str = _reader.GetString()!;
                                var _bytes = Common.Printable.Byteify(_str);
                                if (_bytes.Length != 97) throw new Exception();
                                return (_bytes[0], new Signature(_bytes[1..]));
                            }, options);
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
                                case 3 or 5:
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
                                case 3 or 5:
                                    transaction.TOutputs = ReadArray(ref reader, ScriptTXOutputConverter, options);
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
                                    {
                                        var _tsigs = ReadArray(ref reader, SignatureConverter, options);
                                        transaction.TSignatures = Enumerable.Range(0, _tsigs?.Length ?? 0).Select(x => (byte)x).Zip(_tsigs).ToArray();
                                    }
                                    break;
                                case 5:
                                    transaction.TSignatures = ReadArray<(byte, Signature)>(ref reader, (ref Utf8JsonReader _reader, JsonSerializerOptions _options) =>
                                    {
                                        var _str = _reader.GetString()!;
                                        var _bytes = Common.Printable.Byteify(_str);
                                        if (_bytes.Length != 97) throw new Exception();
                                        return (_bytes[0], new Signature(_bytes[1..]));
                                    }, options);
                                    break;
                                default:
                                    throw new Exception();
                            }
                            break;
                        case "Scripts":
                            transaction._scripts = ReadArray(ref reader, ChainScriptConverter, options);
                            if (transaction._scripts != null && transaction._scripts.Length > 0)
                            {
                                transaction.Scripts = new Dictionary<ScriptAddress, ChainScript>(transaction._scripts.Select(x => new KeyValuePair<ScriptAddress, ChainScript>(new ScriptAddress(x), x)));
                            }
                            else
                            {
                                transaction.Scripts = new();
                            }
                            break;
                        case "Datums":
                            transaction._datums = ReadArray(ref reader, DatumConverter, options);
                            if (transaction._datums != null && transaction._datums.Length > 0)
                            {
                                transaction.Datums = new Dictionary<SHA256, Datum>(transaction._datums.Select(x => new KeyValuePair<SHA256, Datum>(x.Hash(), x)));
                            }
                            else
                            {
                                transaction.Datums = new();
                            }
                            break;
                        case "Redeemers":
                            transaction._redeemers = ReadArray(ref reader, RedeemerConverter, options);
                            if (transaction._redeemers != null && transaction._redeemers.Length > 0)
                            {
                                transaction.Redeemers = new Dictionary<byte, Datum>(transaction._redeemers.Select(x => new KeyValuePair<byte, Datum>(x.Item1, x.Item2)));
                            }
                            else
                            {
                                transaction.Redeemers = new();
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

            if (transaction.Version == 3 || transaction.Version == 5)
            {
                transaction.NumTInputs = transaction.NumInputs;
                transaction.NumTOutputs = transaction.NumOutputs;
            }
            else if (transaction.Version == 2 || transaction.Version == 1 || transaction.Version == 0)
            {
                transaction.NumPInputs = transaction.NumInputs;
                transaction.NumPOutputs = transaction.NumOutputs;
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

            if (value.Version == 5 || value.Version == 4)
            {
                writer.WriteNumber(nameof(value.NumRefInputs), value.NumRefInputs);
                writer.WriteNumber(nameof(value.NumScriptInputs), value.NumScriptInputs);
            }

            writer.WriteNumber(nameof(value.Fee), value.Fee);

            if (value.Version == 3 || value.Version == 4 || value.Version == 5)
            {
                writer.WriteString(value.Version == 4 ? nameof(value.SigningHash) : "InnerHash", value.SigningHash.ToHex());
            }
            else
            {
                writer.WriteString(nameof(value.TransactionKey), value.TransactionKey.ToHex());
            }

            if (value.Version == 3 || value.Version == 4)
            {
                writer.WritePropertyName(nameof(value.ValidityInterval));
                writer.WriteStartArray();
                writer.WriteNumberValue(value.ValidityInterval.Item1);
                writer.WriteNumberValue(value.ValidityInterval.Item2);
                writer.WriteEndArray();
            }

            if (value.Version == 4 || value.Version == 3 || value.Version == 5)
            {
                // write fields as individual elements
                if (value.TInputs != null && value.TInputs.Length > 0)
                {
                    writer.WritePropertyName((value.Version == 3 || value.Version == 5) ? "Inputs" : nameof(value.TInputs));
                    WriteArray(writer, value.TInputs, TTXInputConverter, options);
                }

                // we don't allow ref inputs in version 3 transactions
                if (value.RefInputs != null && value.RefInputs.Length > 0 && value.Version != 3)
                {
                    writer.WritePropertyName(nameof(value.RefInputs));
                    WriteArray(writer, value.RefInputs, TTXInputConverter, options);
                }

                if (value.TOutputs != null && value.TOutputs.Length > 0)
                {
                    writer.WritePropertyName((value.Version == 3 || value.Version == 5) ? "Outputs" : nameof(value.TOutputs));
                    WriteArray(writer, value.TOutputs, ScriptTXOutputConverter, options);
                }

                if (value.TSignatures != null && value.TSignatures.Length > 0)
                {
                    writer.WritePropertyName((value.Version == 3 || value.Version == 5) ? "Signatures" : nameof(value.TSignatures));
                    if (value.Version == 4 || value.Version == 5)
                    {
                        WriteArray(writer, value.TSignatures.Select(x => Common.Printable.Hexify(new byte[] { x.Index }.Concat(x.Signature.Serialize()).ToArray())).ToArray(), StringConverter, options);
                    }
                    else
                    {
                        WriteArray(writer, value.TSignatures.Select(x => x.Signature).ToArray(), SignatureConverter, options);
                    }
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

            if (value.Version == 4 || value.Version == 5)
            {
                if (value._scripts != null && value._datums.Length > 0)
                {
                    writer.WritePropertyName("Scripts");
                    WriteArray(writer, value._scripts, ChainScriptConverter, options);
                }

                if (value._datums != null && value._datums.Length > 0)
                {
                    writer.WritePropertyName("Datums");
                    WriteArray(writer, value._datums, DatumConverter, options);
                }

                if (value._redeemers != null && value._redeemers.Length > 0)
                {
                    writer.WritePropertyName("Redeemers");
                    WriteArray(writer, value._redeemers, RedeemerConverter, options);
                }
            }

            writer.WriteString(nameof(value.TxID), value.TxID.ToHex());

            writer.WriteEndObject();
        }
    }
}
