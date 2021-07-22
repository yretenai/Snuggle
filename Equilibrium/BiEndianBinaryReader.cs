using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Equilibrium.Models;
using JetBrains.Annotations;

namespace Equilibrium {
    [PublicAPI]
    public class BiEndianBinaryReader : BinaryReader {
        public BiEndianBinaryReader(Stream input, bool isBigEndian = true, Encoding? encoding = null, bool leaveOpen = false) :
            base(input, encoding ?? Encoding.UTF8, leaveOpen) {
            IsBigEndian = isBigEndian;
            Encoding = encoding ?? Encoding.UTF8;
        }

        public bool IsBigEndian { get; set; }

        private Encoding Encoding { get; init; }

        protected bool ShouldInvertEndianness => BitConverter.IsLittleEndian ? IsBigEndian : !IsBigEndian;

        public static BiEndianBinaryReader FromSpan(Span<byte> span, bool isBigEndian = true, Encoding? encoding = null) {
            var ms = new MemoryStream(span.ToArray()) { Position = 0 };
            return new BiEndianBinaryReader(ms, isBigEndian, encoding);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Align(int alignment = 4) {
            if (BaseStream.Position % 4 == 0) {
                return;
            }

            var delta = (int) (4 - BaseStream.Position % 4);
            if (BaseStream.CanSeek) {
                BaseStream.Seek(delta, SeekOrigin.Current);
            } else {
                Span<byte> buffer = stackalloc byte[delta];
                BaseStream.Read(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override decimal ReadDecimal() {
            Span<byte> span = stackalloc byte[16];
            Read(span);

            var lo = BinaryPrimitives.ReadInt32LittleEndian(span);
            var mid = BinaryPrimitives.ReadInt32LittleEndian(span[4..]);
            var hi = BinaryPrimitives.ReadInt32LittleEndian(span[8..]);
            var flags = BinaryPrimitives.ReadInt32LittleEndian(span[12..]);
            if (ShouldInvertEndianness) {
                lo = BinaryPrimitives.ReverseEndianness(lo);
                mid = BinaryPrimitives.ReverseEndianness(mid);
                hi = BinaryPrimitives.ReverseEndianness(hi);
                flags = BinaryPrimitives.ReverseEndianness(flags);
            }

            return new decimal(new[] { lo, mid, hi, flags });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override double ReadDouble() {
            Span<byte> span = stackalloc byte[8];
            Read(span);

            var value = BinaryPrimitives.ReadInt64LittleEndian(span);
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            return BitConverter.Int64BitsToDouble(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float ReadSingle() {
            Span<byte> span = stackalloc byte[4];
            Read(span);

            var value = BinaryPrimitives.ReadInt32LittleEndian(span);
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            return BitConverter.Int32BitsToSingle(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Half ReadHalf() {
            Span<byte> span = stackalloc byte[2];
            Read(span);

            var value = BinaryPrimitives.ReadInt16LittleEndian(span);
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            return BitConverter.Int16BitsToHalf(value);
        }

        public override short ReadInt16() {
            Span<byte> span = stackalloc byte[2];
            Read(span);

            var value = BinaryPrimitives.ReadInt16LittleEndian(span);
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            return value;
        }

        public override int ReadInt32() {
            Span<byte> span = stackalloc byte[4];
            Read(span);

            var value = BinaryPrimitives.ReadInt32LittleEndian(span);
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            return value;
        }

        public override long ReadInt64() {
            Span<byte> span = stackalloc byte[8];
            Read(span);

            var value = BinaryPrimitives.ReadInt64LittleEndian(span);
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            return value;
        }

        public override ushort ReadUInt16() {
            Span<byte> span = stackalloc byte[2];
            Read(span);

            var value = BinaryPrimitives.ReadUInt16LittleEndian(span);
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            return value;
        }

        public override uint ReadUInt32() {
            Span<byte> span = stackalloc byte[4];
            Read(span);

            var value = BinaryPrimitives.ReadUInt32LittleEndian(span);
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            return value;
        }

        public override ulong ReadUInt64() {
            Span<byte> span = stackalloc byte[8];
            Read(span);

            var value = BinaryPrimitives.ReadUInt64LittleEndian(span);
            if (ShouldInvertEndianness) {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            return value;
        }

        public string ReadString32() {
            var length = ReadInt32();
            Span<byte> span = new byte[length];
            Read(span);
            return Encoding.GetString(span);
        }

        public string ReadNullString() {
            var sb = new StringBuilder();
            byte b;
            while ((b = ReadByte()) != 0) {
                sb.Append((char) b);
            }

            return sb.ToString();
        }

        public Span<T> ReadArray<T>(int count) where T : struct {
            Span<byte> span = new byte[Unsafe.SizeOf<T>() * count];
            Read(span);
            var value = MemoryMarshal.Cast<byte, T>(span);
            if (ShouldInvertEndianness) {
                for (var i = 0; i < count; ++i) {
                    if (value[i] is IReversibleStruct reversibleStruct) {
                        reversibleStruct.ReverseEndianness();
                        value[i] = (T) reversibleStruct;
                    }
                }
            }

            return value;
        }

        public T ReadStruct<T>() where T : struct {
            Span<byte> span = new byte[Unsafe.SizeOf<T>()];
            Read(span);
            var value = MemoryMarshal.Read<T>(span);
            if (ShouldInvertEndianness && value is IReversibleStruct reversibleStruct) {
                reversibleStruct.ReverseEndianness();
                value = (T) reversibleStruct;
            }

            return value;
        }
    }
}
