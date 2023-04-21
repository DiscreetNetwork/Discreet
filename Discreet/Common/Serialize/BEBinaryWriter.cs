using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Common.Serialize
{
    /// <summary>
    /// An extension of BinaryWriter which uses Big-Endian encoding for numeric types.
    /// </summary>
    public class BEBinaryWriter : BinaryWriter
    {
        private byte[] _buffer; // temporary space for writing primitives, as in parent class.

        protected BEBinaryWriter() : base()
        {
            _buffer = new byte[16];
        }

        public BEBinaryWriter(Stream output) : base(output, new UTF8Encoding(false, true), false)
        {
            _buffer = new byte[16];
        }

        public BEBinaryWriter(Stream output, Encoding encoding) : base(output, encoding, false)
        {
            _buffer = new byte[16];
        }

        public BEBinaryWriter(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen)
        {
            _buffer = new byte[16];
        }

        public override void Write(short value)
        {
            _buffer[0] = (byte)(value >> 8);
            _buffer[1] = (byte)value;
            OutStream.Write(_buffer, 0, 2);
        }

        public override void Write(ushort value)
        {
            _buffer[0] = (byte)(value >> 8);
            _buffer[1] = (byte)value;
            OutStream.Write(_buffer, 0, 2);
        }

        public override void Write(int value)
        {
            _buffer[0] = (byte)(value >> 24);
            _buffer[1] = (byte)(value >> 16);
            _buffer[2] = (byte)(value >> 8);
            _buffer[3] = (byte)value;
            OutStream.Write(_buffer, 0, 4);
        }

        public override void Write(uint value)
        {
            _buffer[0] = (byte)(value >> 24);
            _buffer[1] = (byte)(value >> 16);
            _buffer[2] = (byte)(value >> 8);
            _buffer[3] = (byte)value;
            OutStream.Write(_buffer, 0, 4);
        }

        public override void Write(long value)
        {
            _buffer[0] = (byte)(value >> 56);
            _buffer[1] = (byte)(value >> 48);
            _buffer[2] = (byte)(value >> 40);
            _buffer[3] = (byte)(value >> 32);
            _buffer[4] = (byte)(value >> 24);
            _buffer[5] = (byte)(value >> 16);
            _buffer[6] = (byte)(value >> 8);
            _buffer[7] = (byte)value;
            OutStream.Write(_buffer, 0, 8);
        }

        public override void Write(ulong value)
        {
            _buffer[0] = (byte)(value >> 56);
            _buffer[1] = (byte)(value >> 48);
            _buffer[2] = (byte)(value >> 40);
            _buffer[3] = (byte)(value >> 32);
            _buffer[4] = (byte)(value >> 24);
            _buffer[5] = (byte)(value >> 16);
            _buffer[6] = (byte)(value >> 8);
            _buffer[7] = (byte)value;
            OutStream.Write(_buffer, 0, 8);
        }

        public override void Write(float value)
        {
            BinaryPrimitives.WriteSingleBigEndian(_buffer.AsSpan(0), value);
            OutStream.Write(_buffer, 0, 4);
        }

        public override void Write(double value)
        {
            BinaryPrimitives.WriteDoubleBigEndian(_buffer.AsSpan(0), value);
            OutStream.Write(_buffer, 0, 8);
        }

        // TODO: is this compliant with our serialization standard?
        public override void Write(decimal value) => base.Write(value);

        public override void Write(bool value)
        {
            // base encodes bools as a single byte; 1 for true and 0 for false. This is compliant with our standard.
            base.Write(value);
        }

        public override void Write(string value)
        {
            // Note: This is subject to change in a future refactor. Encoding.UTF8 leaves in a BOM, which we don't need.
            var bytes = Encoding.UTF8.GetBytes(value);
            Write(bytes.Length);
            Write(bytes);
        }
    }
}
