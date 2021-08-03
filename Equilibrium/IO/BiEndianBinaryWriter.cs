using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;

namespace Equilibrium.IO {
    [PublicAPI]
    public class BiEndianBinaryWriter : BinaryWriter {
        public BiEndianBinaryWriter(Stream input, bool isBigEndian = false, bool leaveOpen = false) :
            base(input, Encoding.UTF8, leaveOpen) {
            IsBigEndian = isBigEndian;
            Encoding = Encoding.UTF8;
        }

        public bool IsBigEndian { get; set; }

        public Encoding Encoding { get; private init; }

        protected bool ShouldInvertEndianness => BitConverter.IsLittleEndian ? IsBigEndian : !IsBigEndian;

        public static BiEndianBinaryReader FromArray(byte[] array, bool isBigEndian = false) {
            var ms = new MemoryStream(array) { Position = 0 };
            return new BiEndianBinaryReader(ms, isBigEndian);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Align(int alignment = 4) {
            if (BaseStream.Position % 4 == 0) {
                return;
            }

            var delta = (int) (4 - BaseStream.Position % 4);
            Span<byte> span = stackalloc byte[delta];
            span.Fill(0);
            Write(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write(decimal value) {
            if (ShouldInvertEndianness) {
                throw new NotSupportedException("Cannot invert the endianness of decimals");
            }

            base.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write(double value) {
            var intValue = BitConverter.DoubleToInt64Bits(value);
            if (ShouldInvertEndianness) {
                intValue = BinaryPrimitives.ReverseEndianness(intValue);
            }

            base.Write(intValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write(float value) {
            var intValue = BitConverter.SingleToInt32Bits(value);
            if (ShouldInvertEndianness) {
                intValue = BinaryPrimitives.ReverseEndianness(intValue);
            }

            base.Write(intValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write(Half value) {
            var intValue = BitConverter.HalfToInt16Bits(value);
            if (ShouldInvertEndianness) {
                intValue = BinaryPrimitives.ReverseEndianness(intValue);
            }

            base.Write(intValue);
        }

        public override void Write(short value) {
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            base.Write(value);
        }

        public override void Write(int value) {
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            base.Write(value);
        }

        public override void Write(long value) {
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            base.Write(value);
        }

        public override void Write(ushort value) {
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            base.Write(value);
        }

        public override void Write(uint value) {
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            base.Write(value);
        }

        public override void Write(ulong value) {
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            base.Write(value);
        }

        public void WriteString32(string value) {
            var length = value.Length;

            if (ShouldInvertEndianness) {
                length = BinaryPrimitives.ReverseEndianness(length);
            }

            base.Write(length);
            Write(Encoding.GetBytes(value));
        }

        public void WriteNullString(string value) {
            Write(Encoding.GetBytes(value));
            Write((byte) 0);
        }

        public void WriteArray<T>(T[] array) where T : struct {
            if (ShouldInvertEndianness) {
                throw new NotSupportedException("Cannot invert endianness of arrays");
            }

            Write(MemoryMarshal.Cast<T, byte>(array));
        }

        public void WriteArray<T>(Span<T> span) where T : struct {
            if (ShouldInvertEndianness) {
                throw new NotSupportedException("Cannot invert endianness of arrays");
            }

            Write(MemoryMarshal.Cast<T, byte>(span));
        }

        public void WriteMemory(Memory<byte>? memory) {
            if (ShouldInvertEndianness) {
                throw new NotSupportedException("Cannot invert endianness of structs");
            }

            if (memory == null) {
                Write(0);
            } else {
                Write(memory.Value.Span);
            }
        }

        public void WriteMemory<T>(Memory<T>? memory) where T : struct {
            if (ShouldInvertEndianness) {
                throw new NotSupportedException("Cannot invert endianness of structs");
            }

            if (memory == null) {
                Write(0);
            } else {
                Write(MemoryMarshal.Cast<T, byte>(memory.Value.Span));
            }
        }

        public void WriteStruct<T>(T value) where T : struct {
            if (ShouldInvertEndianness) {
                throw new NotSupportedException("Cannot invert endianness of structs");
            }

            Span<byte> span = new byte[Unsafe.SizeOf<T>()];
            MemoryMarshal.Write(span, ref value);
            Write(span);
        }
    }
}
