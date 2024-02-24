using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.Text.Json.Serialization;
using System.IO;
using Discreet.Common;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace Discreet.Coin.Models
{
    public class TXOutput : IHashable, ISerializable
    {
        public SHA256 TransactionSrc { get; set; }
        public Key UXKey { get; set; }
        public Key Commitment { get; set; }
        public ulong Amount { get; set; }

        public uint Index { get; set; } /* unused mostly except for CreateTransaction() */

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is TXOutput tout)
            {
                if (tout.UXKey == default || !tout.UXKey.Equals(UXKey)) return false;
                if (tout.Commitment == default || !tout.Commitment.Equals(Commitment)) return false;
                if (tout.Amount != Amount) return false;

                if (TransactionSrc != default && tout.TransactionSrc != default)
                {
                    if (TransactionSrc.Equals(tout.TransactionSrc)) return true;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        public TXOutput()
        {
        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteSHA256(TransactionSrc); 
            writer.WriteKey(UXKey);
            writer.WriteKey(Commitment);
            writer.Write(Amount);
        }

        public void TXMarshal(BEBinaryWriter writer)
        {
            writer.WriteKey(UXKey);
            writer.WriteKey(Commitment);
            writer.Write(Amount);
        }

        public string Readable()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Converters.TXOutputConverter());

            return JsonSerializer.Serialize(this, typeof(TXOutput), options);
        }

        public static uint GetSize()
        {
            return 104;
        }

        public int Size => (int)GetSize();

        public static uint GetTXSize() => 104;
        public int TXSize => (int)GetTXSize();

        public void Deserialize(ref MemoryReader reader)
        {
            TransactionSrc = reader.ReadSHA256();
            UXKey = reader.ReadKey();
            Commitment = reader.ReadKey();
            Amount = reader.ReadUInt64();
        }

        public void TXUnmarshal(ref MemoryReader reader)
        {
            UXKey = reader.ReadKey();
            Commitment = reader.ReadKey();
            Amount = reader.ReadUInt64();
        }
    }
}
