using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;

namespace Equilibrium.IO {
    [PublicAPI]
    public class BiEndianBinaryReader : BinaryReader {
        public BiEndianBinaryReader(Stream input, bool isBigEndian = false, bool leaveOpen = false) :
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
            BaseStream.Seek(delta, SeekOrigin.Current);
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
            return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
        }

        public override int ReadInt32() {
            Span<byte> span = stackalloc byte[4];
            Read(span);

            var value = BinaryPrimitives.ReadInt32LittleEndian(span);
            return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
        }

        public override long ReadInt64() {
            Span<byte> span = stackalloc byte[8];
            Read(span);

            var value = BinaryPrimitives.ReadInt64LittleEndian(span);
            return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
        }

        public override ushort ReadUInt16() {
            Span<byte> span = stackalloc byte[2];
            Read(span);

            var value = BinaryPrimitives.ReadUInt16LittleEndian(span);
            return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
        }

        public override uint ReadUInt32() {
            Span<byte> span = stackalloc byte[4];
            Read(span);

            var value = BinaryPrimitives.ReadUInt32LittleEndian(span);
            return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
        }

        public override ulong ReadUInt64() {
            Span<byte> span = stackalloc byte[8];
            Read(span);

            var value = BinaryPrimitives.ReadUInt64LittleEndian(span);
            return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
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
                throw new NotSupportedException();
            }

            return value;
        }

        public T ReadStruct<T>() where T : struct {
            Span<byte> span = new byte[Unsafe.SizeOf<T>()];
            Read(span);
            var value = MemoryMarshal.Read<T>(span);
            if (ShouldInvertEndianness) {
                throw new NotSupportedException();
            }

            return value;
        }
    }
}
