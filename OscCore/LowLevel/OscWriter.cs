// Copyright (c) Tilde Love Project. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OscCore.LowLevel
{
    public struct OscWriter
    {
        private OscArgumentBuffer argumentBuffer;
        private int argumentBufferCount;

        private readonly ArraySegment<byte> buffer;
        private int count;

        /// <summary>
        /// The writer's position in the buffer.
        /// </summary>
        public int Position { get; set; }

        private WriterState state;

        public OscWriter(ArraySegment<byte> buffer)
        {
            this.buffer = buffer;
            if (buffer.Array == null)
                throw new ArgumentNullException(nameof(buffer));
            argumentBufferCount = 0;
            count = 0;
            state = WriterState.NotStarted;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartBundle(string ident, ref OscTimeTag timestamp)
        {
            StartMessage();

            // write the address
            WriteDirect(Encoding.UTF8.GetBytes(ident));

            // write null terminator
            Write((byte)0);

            WritePadding();

            Write(unchecked((long)timestamp.Value));
            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartMessage()
        {
            state = WriterState.Address;
            count = 0;
            argumentBufferCount = 0;
            //Position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAddress(string address)
        {
            CheckWriterState(WriterState.Address);

            // write the address
            WriteDirect(Encoding.UTF8.GetBytes(address));

            // write null terminator
            Write((byte)0);

            WritePadding();

            // write the comma for the type-tag
            Write((byte)',');

            Flush();

            state = WriterState.TypeTag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBlob(byte[] buffer)
        {
            CheckWriterState(WriterState.Arguments);

            // write length 
            Write(buffer.Length);
            Flush();

            // write bytes 
            WriteDirect(buffer);

            WritePadding();
            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBlob(ArraySegment<byte> buffer)
        {
            CheckWriterState(WriterState.Arguments);

            // write length 
            Write(buffer.Count);
            Flush();

            // write bytes 
            WriteDirect(buffer);

            WritePadding();
            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBundleMessageLength(int messageSizeInBytes)
        {
            Write(messageSizeInBytes);
            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteChar(byte value)
        {
            CheckWriterState(WriterState.Arguments);

            Write(value);
            Write((byte)0);
            Write((byte)0);
            Write((byte)0);

            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteColor(ref OscColor value)
        {
            CheckWriterState(WriterState.Arguments);

            int intValue = (value.R << 24) |
                           (value.G << 16) |
                           (value.B << 8) |
                           (value.A << 0);

            Write(intValue);

            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDouble(double value)
        {
            CheckWriterState(WriterState.Arguments);
            Write(BitConverter.DoubleToInt64Bits(value));
            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFloat(float value)
        {
            CheckWriterState(WriterState.Arguments);
            Write(BitConverter.SingleToInt32Bits(value));
            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt(int value)
        {
            CheckWriterState(WriterState.Arguments);

            Write(value);

            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLong(long value)
        {
            CheckWriterState(WriterState.Arguments);

            Write(value);

            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteMidi(ref OscMidiMessage value)
        {
            CheckWriterState(WriterState.Arguments);

            Write(unchecked((int)value.FullMessage));

            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string value)
        {
            CheckWriterState(WriterState.Arguments);

            // write the address
            WriteDirect(Encoding.UTF8.GetBytes(value));
            // write null terminator
            Write((byte)0);

            WritePadding();
            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSymbol(ref OscSymbol value)
        {
            CheckWriterState(WriterState.Arguments);

            // write the address
            WriteDirect(Encoding.UTF8.GetBytes(value.Value));
            // write null terminator
            Write((byte)0);

            WritePadding();
            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTimeTag(ref OscTimeTag value)
        {
            CheckWriterState(WriterState.Arguments);

            Write(unchecked((long)value.Value));

            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTypeTag(OscToken token)
        {
            CheckWriterState(WriterState.TypeTag);

            FlushConditionally();

            switch (token)
            {
                case OscToken.Char:
                    Write((byte)'c');
                    break;
                case OscToken.True:
                    Write((byte)'T');
                    break;
                case OscToken.False:
                    Write((byte)'F');
                    break;
                case OscToken.String:
                    Write((byte)'s');
                    break;
                case OscToken.Symbol:
                    Write((byte)'S');
                    break;
                case OscToken.Impulse:
                    Write((byte)'I');
                    break;
                case OscToken.Null:
                    Write((byte)'N');
                    break;
                case OscToken.Int:
                    Write((byte)'i');
                    break;
                case OscToken.Long:
                    Write((byte)'h');
                    break;
                case OscToken.Float:
                    Write((byte)'f');
                    break;
                case OscToken.Double:
                    Write((byte)'d');
                    break;
                case OscToken.TimeTag:
                    Write((byte)'t');
                    break;
                case OscToken.Blob:
                    Write((byte)'b');
                    break;
                case OscToken.Color:
                    Write((byte)'r');
                    break;
                case OscToken.Midi:
                    Write((byte)'m');
                    break;
                case OscToken.ArrayStart:
                    Write((byte)'[');
                    break;
                case OscToken.ArrayEnd:
                    Write((byte)']');
                    break;
                default:
                    throw new OscException(OscError.UnexpectedToken, $"Unexpected token {token}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTypeTagEnd()
        {
            CheckWriterState(WriterState.TypeTag);

            // write null terminator
            Write((byte)0);

            WritePadding();
            Flush();

            state = WriterState.Arguments;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int CalculatePadding()
        {
            int nullCount = 4 - count % 4;

            return nullCount < 4 ? nullCount : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckWriterState(WriterState requiredState)
        {
            if (state != requiredState)
            {
                throw new OscException(OscError.UnexpectedWriterState, $"Unexpected writer state {state}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Flush()
        {
            if (argumentBufferCount == 0)
            {
                return;
            }

            argumentBuffer.Bytes[..argumentBufferCount].CopyTo(buffer.AsSpan(Position));
            Position += argumentBufferCount;
            argumentBufferCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FlushConditionally()
        {
            if (argumentBufferCount + 4 > argumentBuffer.Length)
            {
                Flush();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(byte value)
        {
            argumentBuffer[argumentBufferCount++] = value;
            count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(int value)
        {
            int dataSize = Unsafe.SizeOf<int>();
            if (argumentBufferCount + dataSize > argumentBuffer.Length)
                throw new IndexOutOfRangeException();

            value = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
            Unsafe.WriteUnaligned(ref argumentBuffer[argumentBufferCount], value);
            argumentBufferCount += dataSize;
            count += dataSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(long value)
        {
            int dataSize = Unsafe.SizeOf<long>();
            if (argumentBufferCount + dataSize > argumentBuffer.Length)
                throw new IndexOutOfRangeException();

            value = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
            Unsafe.WriteUnaligned(ref argumentBuffer[argumentBufferCount], value);
            argumentBufferCount += dataSize;
            count += dataSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteDirect(ReadOnlySpan<byte> bytes)
        {
            bytes.CopyTo(buffer.AsSpan(Position));
            Position += bytes.Length;
            count += bytes.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WritePadding()
        {
            int nullCount = CalculatePadding();

            for (int i = 0; i < nullCount; i++)
            {
                argumentBuffer[argumentBufferCount++] = 0;
                count++;
            }
        }

        private enum WriterState
        {
            NotStarted,
            Address,
            TypeTag,
            Arguments
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct OscArgumentBuffer
    {
        [FieldOffset(0x0)] public ulong a;
        [FieldOffset(0x8)] public ulong b;

        // Maybe not the fastest, but probably not that bad...
        // I guess we could just ref struct the whole thing and cache this span...
        public ref byte this[int index] => ref MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1))[index];

        public readonly int Length => 16;

        public ReadOnlySpan<byte> Bytes => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
    }
}