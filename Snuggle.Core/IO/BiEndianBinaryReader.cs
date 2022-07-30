using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Snuggle.Core.IO;

public class BiEndianBinaryReader : BinaryReader {
    public BiEndianBinaryReader(Stream input, bool isBigEndian = false, bool leaveOpen = false) : base(input, Encoding.UTF8, leaveOpen) {
        IsBigEndian = isBigEndian;
        Encoding = Encoding.UTF8;
    }

    public bool IsBigEndian { get; set; }

    public Encoding Encoding { get; }

    protected bool ShouldInvertEndianness => BitConverter.IsLittleEndian ? IsBigEndian : !IsBigEndian;
    public long Unconsumed => BaseStream.Length - BaseStream.Position;

    public static BiEndianBinaryReader FromArray(byte[] array, bool isBigEndian = false) {
        var ms = new MemoryStream(array) { Position = 0 };
        return new BiEndianBinaryReader(ms, isBigEndian);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Align(int alignment = 4) {
        if (BaseStream.Position % alignment == 0) {
            return;
        }

        var delta = (int) (alignment - BaseStream.Position % alignment);
        if (BaseStream.Position + delta > BaseStream.Length) {
            return;
        }

        BaseStream.Seek(delta, SeekOrigin.Current);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override decimal ReadDecimal() {
        Span<byte> span = stackalloc byte[16];
        BaseStream.ReadExactly(span);

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
        BaseStream.ReadExactly(span);

        var value = BinaryPrimitives.ReadInt64LittleEndian(span);
        if (ShouldInvertEndianness) {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        return BitConverter.Int64BitsToDouble(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float ReadSingle() {
        Span<byte> span = stackalloc byte[4];
        BaseStream.ReadExactly(span);

        var value = BinaryPrimitives.ReadInt32LittleEndian(span);
        if (ShouldInvertEndianness) {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        return BitConverter.Int32BitsToSingle(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Half ReadHalf() {
        Span<byte> span = stackalloc byte[2];
        BaseStream.ReadExactly(span);

        var value = BinaryPrimitives.ReadInt16LittleEndian(span);
        if (ShouldInvertEndianness) {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        return BitConverter.Int16BitsToHalf(value);
    }

    public override short ReadInt16() {
        Span<byte> span = stackalloc byte[2];
        BaseStream.ReadExactly(span);

        var value = BinaryPrimitives.ReadInt16LittleEndian(span);
        return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public override int ReadInt32() {
        Span<byte> span = stackalloc byte[4];
        BaseStream.ReadExactly(span);

        var value = BinaryPrimitives.ReadInt32LittleEndian(span);
        return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public override long ReadInt64() {
        Span<byte> span = stackalloc byte[8];
        BaseStream.ReadExactly(span);

        var value = BinaryPrimitives.ReadInt64LittleEndian(span);
        return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public override ushort ReadUInt16() {
        Span<byte> span = stackalloc byte[2];
        BaseStream.ReadExactly(span);

        var value = BinaryPrimitives.ReadUInt16LittleEndian(span);
        return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public override uint ReadUInt32() {
        Span<byte> span = stackalloc byte[4];
        BaseStream.ReadExactly(span);

        var value = BinaryPrimitives.ReadUInt32LittleEndian(span);
        return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public override ulong ReadUInt64() {
        Span<byte> span = stackalloc byte[8];
        BaseStream.ReadExactly(span);

        var value = BinaryPrimitives.ReadUInt64LittleEndian(span);
        return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public string ReadString32(int align = 4) {
        var length = ReadInt32();
        if (length < 0) {
            throw new InvalidDataException();
        }

        Span<byte> span = new byte[length];
        BaseStream.ReadExactly(span);
        if (align > 1) {
            Align(align);
        }

        return Encoding.GetString(span);
    }

    public string ReadNullString(int maxLength = 0) {
        var sb = new StringBuilder();
        byte b;
        while ((b = ReadByte()) != 0) {
            sb.Append((char) b);

            if (maxLength > 0 && sb.Length >= maxLength) {
                break;
            }
        }

        return sb.ToString();
    }

    public Span<byte> ReadSpan(int count) {
        Span<byte> span = new byte[count];
        BaseStream.ReadExactly(span);
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of arrays");
        }

        return span;
    }

    public override bool ReadBoolean() {
        try {
            return ReadByte() == 1;
        } catch {
            return false;
        }
    }

    public Span<T> ReadSpan<T>(int count) where T : struct {
        Span<T> span = new T[count];
        BaseStream.ReadExactly(MemoryMarshal.AsBytes(span));
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of arrays");
        }

        return span;
    }

    public byte[] ReadArray(int count) {
        var array = new byte[count];
        BaseStream.ReadExactly(array.AsSpan());
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of arrays");
        }

        return array;
    }

    public T[] ReadArray<T>(int count) where T : struct {
        var array = new T[count];
        BaseStream.ReadExactly(MemoryMarshal.AsBytes(array.AsSpan()));
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of arrays");
        }

        return array;
    }

    public Memory<byte> ReadMemory(long count) {
        Memory<byte> memory = new byte[count];
        BaseStream.ReadExactly(memory.Span);
        return memory;
    }

    public Memory<T> ReadMemory<T>(long count) where T : struct {
        Memory<T> memory = new T[count];
        BaseStream.ReadExactly(MemoryMarshal.AsBytes(memory.Span));
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of arrays");
        }

        return memory;
    }

    public T ReadStruct<T>() where T : struct {
        Span<T> span = new T[1];
        BaseStream.ReadExactly(MemoryMarshal.AsBytes(span));
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of structs");
        }

        return span[0];
    }
}
