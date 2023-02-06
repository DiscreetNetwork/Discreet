using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Discreet.Common.Serialization
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
    }
}
