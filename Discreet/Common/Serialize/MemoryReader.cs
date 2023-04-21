using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace Discreet.Common.Serialize
{
    public ref struct MemoryReader
    {
        private readonly ReadOnlyMemory<byte> memory;
        private readonly ReadOnlySpan<byte> span;
        private int pos = 0;

        public int Position => pos;

        public MemoryReader(ReadOnlyMemory<byte> memory)
        {
            this.memory = memory;
            this.span = memory.Span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsurePosition(int move) 
        {
            if (pos + move > span.Length)
            {
                throw new FormatException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Peek()
        {
            EnsurePosition(1);
            return span[pos];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool()
        {
            return ReadUInt8() switch
            {
                0 => false,
                1 => true,
                _ => throw new FormatException()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadInt8()
        {
            EnsurePosition(1);
            byte b = span[pos++];
            return unchecked((sbyte)b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadUInt8()
        {
            EnsurePosition(1);
            return span[pos++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            EnsurePosition(sizeof(short));
            var res = BinaryPrimitives.ReadInt16BigEndian(span[pos..]);
            pos += sizeof(short);
            return res;
        }

        public short ReadInt16BigEndian() => ReadInt16();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16LittleEndian()
        {
            EnsurePosition(sizeof(short));
            var res = BinaryPrimitives.ReadInt16LittleEndian(span[pos..]);
            pos += sizeof(short);
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            EnsurePosition(sizeof(ushort));
            var res = BinaryPrimitives.ReadUInt16BigEndian(span[pos..]);
            pos += sizeof(ushort);
            return res;
        }

        public ushort ReadUInt16BigEndian() => ReadUInt16();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16LittleEndian()
        {
            EnsurePosition(sizeof(ushort));
            var res = BinaryPrimitives.ReadUInt16LittleEndian(span[pos..]);
            pos += sizeof(ushort);
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            EnsurePosition(sizeof(int));
            var res = BinaryPrimitives.ReadInt32BigEndian(span[pos..]);
            pos += sizeof(int);
            return res;
        }

        public int ReadInt32BigEndian() => ReadInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32LittleEndian()
        {
            EnsurePosition(sizeof(int));
            var res = BinaryPrimitives.ReadInt32LittleEndian(span[pos..]);
            pos += sizeof(int);
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            EnsurePosition(sizeof(uint));
            var res = BinaryPrimitives.ReadUInt32BigEndian(span[pos..]);
            pos += sizeof(uint);
            return res;
        }

        public uint ReadUInt32BigEndian() => ReadUInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32LittleEndian()
        {
            EnsurePosition(sizeof(uint));
            var res = BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
            pos += sizeof(uint);
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            EnsurePosition(sizeof(long));
            var res = BinaryPrimitives.ReadInt64BigEndian(span[pos..]);
            pos += sizeof(long);
            return res;
        }

        public long ReadInt64BigEndian() => ReadInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64LittleEndian()
        {
            EnsurePosition(sizeof(long));
            var res = BinaryPrimitives.ReadInt64LittleEndian(span[pos..]);
            pos += sizeof(long);
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            EnsurePosition(sizeof(ulong));
            var res = BinaryPrimitives.ReadUInt64BigEndian(span[pos..]);
            pos += sizeof(ulong);
            return res;
        }

        public ulong ReadUInt64BigEndian() => ReadUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64LittleEndian()
        {
            EnsurePosition(sizeof(ulong));
            var res = BinaryPrimitives.ReadUInt64LittleEndian(span[pos..]);
            pos += sizeof(ulong);
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadFloat()
        {
            EnsurePosition(sizeof(float));
            var res = BinaryPrimitives.ReadSingleBigEndian(span[pos..]);
            pos += sizeof(float);
            return res;
        }

        public float ReadFloatBigEndian() => ReadFloat();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadFloatLittleEndian()
        {
            EnsurePosition(sizeof(float));
            var res = BinaryPrimitives.ReadSingleLittleEndian(span[pos..]);
            pos += sizeof(float);
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            EnsurePosition(sizeof(double));
            var res = BinaryPrimitives.ReadDoubleBigEndian(span[pos..]);
            pos += sizeof(double);
            return res;
        }

        public double ReadDoubleBigEndian() => ReadDouble();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDoubleLittleEndian()
        {
            EnsurePosition(sizeof(double));
            var res = BinaryPrimitives.ReadDoubleLittleEndian(span[pos..]);
            pos += sizeof(double);
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLengthPrefixedString()
        {
            int length = ReadInt32();
            EnsurePosition(length);
            var res = Encoding.UTF8.GetString(span.Slice(pos, length));
            pos += length;
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<byte> ReadMemory(int count)
        {
            EnsurePosition(count);
            var res = memory.Slice(pos, count);
            pos += count;
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<byte> ReadMemory()
        {
            return ReadMemory(ReadInt32());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<byte> ReadToEnd()
        {
            var res = memory[pos..];
            pos = memory.Length;
            return res;
        }
    }
}
