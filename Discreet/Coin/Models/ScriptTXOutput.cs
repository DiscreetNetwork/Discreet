using Discreet.Cipher;
using Discreet.Coin.Script;
using Discreet.Common.Serialize;
using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Discreet.Coin.Models
{
    /// <summary>
    /// Defines a transparent Tx output that supports scripting
    /// </summary>
    public class ScriptTXOutput : TTXOutput
    {
        public Datum Datum { get; set; }
        public SHA256? DatumHash { get; set; } = null;
        public ChainScript ReferenceScript { get; set; } = null;

        public ScriptTXOutput() { }

        public ScriptTXOutput(TTXOutput other)
        {
            Datum = null;
            DatumHash = null;
            ReferenceScript = null;
        }

        public ScriptTXOutput(SHA256 txsrc, TAddress address, ulong amount) : base(txsrc, address, amount)
        {
            Datum = Datum.Default();
            DatumHash = null;
            ReferenceScript = null;
        }

        public new void Serialize(BEBinaryWriter writer)
        {
            writer.WriteSHA256(TransactionSrc);
            writer.WriteByteArray(Address.Bytes(), false);
            writer.Write(Amount);
            writer.Write((Datum == null && DatumHash == null) ? (byte)0 : (Datum == null ? (byte)1 : (byte)2));
            if (DatumHash != null)
            {
                writer.WriteSHA256(DatumHash.Value);
            }
            else
            {
                writer.Write(Datum);
            }

            if (ReferenceScript is not null) writer.Write(ReferenceScript);
            else writer.Write(0u);
        }

        public new void TXMarshal(BEBinaryWriter writer)
        {
            writer.WriteByteArray(Address.Bytes(), false);
            writer.Write(Amount);
            writer.Write((Datum == null && DatumHash == null) ? (byte)0 : (Datum == null ? (byte)1 : (byte)2));
            if (DatumHash != null)
            {
                writer.WriteSHA256(DatumHash.Value);
            }
            else
            {
                writer.Write(Datum);
            }

            if (ReferenceScript is not null) writer.Write(ReferenceScript);
            else writer.Write(0u);
        }

        public new void TXMarshal(byte[] bytes, int offset = 0) => TXMarshal(new BEBinaryWriter(new MemoryStream(bytes, offset, TXSize)));

        public new byte[] TXMarshal()
        {
            byte[] rv = new byte[TXSize];
            TXMarshal(rv);
            return rv;
        }

        public new string Readable()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Converters.TTXOutputConverter());

            return JsonSerializer.Serialize(this, typeof(TTXOutput), options);
        }

        public new void Deserialize(ref MemoryReader reader)
        {
            TransactionSrc = reader.ReadSHA256();
            Address = new(reader.ReadByteArray(25));
            Amount = reader.ReadUInt64();

            var type = reader.ReadUInt8();
            if (type == 0)
            {
                Datum = null;
            }
            else if (type == 1)
            {
                DatumHash = new SHA256(reader.ReadByteArray(32), false);
            }
            else if (type == 2)
            {
                Datum = reader.ReadSerializable<Datum>();
            }
            else
            {
                throw new Exception("Failed to deserialize datum in output: unknown type code!");
            }

            ReferenceScript = reader.ReadSerializable<ChainScript>();
            if (ReferenceScript.Version == 0)
            {
                ReferenceScript = null;
            }
        }

        public new void TXUnmarshal(ref MemoryReader reader)
        {
            Address = new(reader.ReadByteArray(25));
            Amount = reader.ReadUInt64();

            var type = reader.ReadUInt8();
            if (type == 0)
            {
                Datum = null;
            }
            else if (type == 1)
            {
                DatumHash = new SHA256(reader.ReadByteArray(32), false);
            }
            else
            {
                Datum = reader.ReadSerializable<Datum>();
            }

            ReferenceScript = reader.ReadSerializable<ChainScript>();
            if (ReferenceScript.Version == 0)
            {
                ReferenceScript = null;
            }
        }

        public new int Size => 65 + ((Datum == null && DatumHash == null) ? 1 : (Datum == null ? 33 : (Datum.Size + 1))) + ReferenceScript?.Size ?? 4;
        public new int TXSize => Size - 32;
    }
}
