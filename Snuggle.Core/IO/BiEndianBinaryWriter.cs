using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;

namespace Snuggle.Core.IO;

[PublicAPI]
public class BiEndianBinaryWriter : BinaryWriter {
    public BiEndianBinaryWriter(Stream input, bool isBigEndian = false, bool leaveOpen = false) : base(input, Encoding.UTF8, leaveOpen) {
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

    public void WriteString32(string value, int align = 4) {
        var length = value.Length;

        if (ShouldInvertEndianness) {
            length = BinaryPrimitives.ReverseEndianness(length);
        }

        base.Write(length);
        Write(Encoding.GetBytes(value));

        if (align > 1) {
            Align(align);
        }
    }

    public void WriteNullString(string value) {
        Write(Encoding.GetBytes(value));
        Write((byte) 0);
    }

    public void WriteArray<T>(IEnumerable<T> enumerable, bool writeCount = true) where T : struct {
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of arrays");
        }

        var array = enumerable as T[] ?? enumerable.ToArray();

        if (writeCount) {
            Write(array.Length);
        }

        Write(MemoryMarshal.AsBytes(array.AsSpan()));
    }

    public void WriteArray<T>(Span<T> span, bool writeCount = true) where T : struct {
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of arrays");
        }

        if (writeCount) {
            Write(span.Length);
        }

        Write(MemoryMarshal.AsBytes(span));
    }

    public void WriteMemory(Memory<byte>? memory, bool writeCount = true) {
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of structs");
        }

        if (writeCount) {
            Write(memory?.Length ?? 0);
        }

        if (memory != null) {
            Write(memory.Value.Span);
        }
    }

    public void WriteMemory<T>(Memory<T>? memory, bool writeCount = true) where T : struct {
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of structs");
        }

        if (writeCount) {
            Write(memory?.Length ?? 0);
        }

        if (memory != null) {
            Write(MemoryMarshal.AsBytes(memory.Value.Span));
        }
    }

    public void WriteStruct<T>(T value) where T : struct {
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of structs");
        }

        Span<T> span = new[] { value };
        Write(MemoryMarshal.AsBytes(span));
    }
}
