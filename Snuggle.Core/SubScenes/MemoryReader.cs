using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Snuggle.Core.SubScenes;

public class MemoryReader {
    public MemoryReader(Memory<byte> data) => Data = data;

    public Memory<byte> Data { get; }
    public int Position { get; set; }
    public int Length => Data.Length;
    public int Remaining => Data.Length - Position;

    public T Read<T>() where T : struct {
        var value = MemoryMarshal.Read<T>(Data.Span[Position..]);
        Position += Unsafe.SizeOf<T>();
        return value;
    }

    public T ReadClass<T>(params object?[] extra) where T : class, new() {
        var args = new object?[extra.Length + 1];
        args[0] = this;
        extra.CopyTo(args, 1);
        return (Activator.CreateInstance(typeof(T), args) as T)!;
    }

    public bool ReadBoolean() {
        var value = Read<uint>();
        if (value > 1) {
            throw new InvalidDataException($"Expected 0 or 1 for a boolean value, got {value}");
        }

        return value == 1;
    }

    public Memory<T> ReadBlobArray<T>() where T : struct {
        var pos = Position;
        var relOffset = Read<int>();
        var count = Read<int>();

        var value = new T[count].AsMemory();
        var size = Unsafe.SizeOf<T>() * count;
        Data.Span.Slice(pos + relOffset, size).CopyTo(MemoryMarshal.AsBytes(value.Span));
        return value;
    }

    public Memory<T>[] ReadBlobArray2D<T>() where T : struct {
        var pos = Position;
        var relOffset = Read<int>();
        var count = Read<int>();
        var value = new Memory<T>[count];
        var tmp = Position;
        Position = pos + relOffset;
        for (var i = 0; i < count; ++i) {
            value[i] = ReadBlobArray<T>();
        }

        Position = tmp;
        return value;
    }

    public T[] ReadBlobClassArray<T>(params object?[] extra) where T : class, new() {
        var pos = Position;
        var relOffset = Read<int>();
        var count = Read<int>();
        var tmp = Position;
        Position = pos + relOffset;

        var value = new T[count];
        var type = typeof(T);
        var args = new object?[extra.Length + 1];
        args[0] = this;
        extra.CopyTo(args, 1);
        for (var index = 0; index < value.Length; ++index) {
            value[index] = (T) Activator.CreateInstance(type, args)!;
        }

        Position = tmp;
        return value;
    }

    public T[][] ReadBlobClassArray2D<T>(params object?[] extra) where T : class, new() {
        var pos = Position;
        var relOffset = Read<int>();
        var count = Read<int>();
        var value = new T[count][];
        var tmp = Position;
        Position = pos + relOffset;
        for (var i = 0; i < count; ++i) {
            value[i] = ReadBlobClassArray<T>(extra);
        }

        Position = tmp;
        return value;
    }

    public Memory<T> ReadArray<T>(int? count = null) where T : struct {
        count ??= Read<int>();
        var value = new T[count.Value].AsMemory();
        var size = Unsafe.SizeOf<T>() * count.Value;
        Data.Span.Slice(Position, size).CopyTo(MemoryMarshal.AsBytes(value.Span));
        Position += size;
        return value;
    }

    public T[] ReadClassArray<T>(int? count = null, params object?[] extra) where T : class, new() {
        count ??= Read<int>();
        var value = new T[count.Value];
        var type = typeof(T);
        var args = new object?[extra.Length + 1];
        args[0] = this;
        extra.CopyTo(args, 1);
        for (var index = 0; index < value.Length; ++index) {
            value[index] = (T) Activator.CreateInstance(type, args)!;
        }

        return value;
    }

    public string ReadBlobString() {
        var pos = Position;
        var relOffset = Read<int>();
        var count = Read<int>();
        var value = Encoding.UTF8.GetString(Data.Span.Slice(pos + relOffset, count));
        return value;
    }

    public string[] ReadBlobStringArray() {
        var pos = Position;
        var relOffset = Read<int>();
        var count = Read<int>();
        var value = new string[count];
        var tmp = Position;
        Position = pos + relOffset;
        for (var i = 0; i < count; ++i) {
            value[i] = ReadBlobString();
        }

        Position = tmp;
        return value;
    }

    public string ReadString(int? count = null) {
        count ??= Read<int>();
        var value = Encoding.UTF8.GetString(Data.Span.Slice(Position, count.Value));
        Position += count.Value;
        return value;
    }

    public string ReadCString() {
        var count = Data.Span[Position..].IndexOf((byte) 0);
        if (count == -1) {
            count = Data.Length;
        }

        var value = Encoding.UTF8.GetString(Data.Span.Slice(Position, count));
        Position += count;
        return value;
    }

    public MemoryReader Partition(int pos, int size) {
        if (size == -1) {
            size = Data.Length - pos;
        }
        return new MemoryReader(Data.Slice(pos, size));
    }

    public MemoryReader Partition(int count) {
        var pos = Position;
        Position += count;
        return Partition(pos, count);
    }

    public void Align(int v = 16) {
        Position = unchecked(Position + (v - 1)) & ~(v - 1);
    }
}
