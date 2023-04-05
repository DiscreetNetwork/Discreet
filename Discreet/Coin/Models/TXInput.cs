using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.IO;
using Discreet.Common;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;

namespace Discreet.Coin.Models
{
    public class TXInput : IHashable
    {
        public uint[] Offsets { get; set; }

        public Key KeyImage { get; set; }

        public void Deserialize(ref MemoryReader reader)
        {
            Offsets = reader.ReadUInt32Array(len: 64);
            KeyImage = reader.ReadKey();
        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteUInt32Array(Offsets, false);
            writer.WriteKey(KeyImage);
        }

        public string Readable()
        {
            return Discreet.Readable.TXInput.ToReadable(this);
        }

        public object ToReadable()
        {
            return new Readable.TXInput(this);
        }

        public static TXInput FromReadable(string json)
        {
            return Discreet.Readable.TXInput.FromReadable(json);
        }

        public static uint GetSize()
        {
            return 4 * 64 + 32;
        }

        public int Size => (int)GetSize();
    }
}
