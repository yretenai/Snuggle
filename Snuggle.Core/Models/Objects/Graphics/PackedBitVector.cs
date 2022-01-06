using System;
using System.IO;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Snuggle.Core.Exceptions;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Graphics;

[PublicAPI]
public record PackedBitVector(uint Count, byte BitSize) {
    private long DataStart { get; init; } = -1;
    public float Range { get; init; } = 1.0f;
    public float Start { get; init; }
    public bool HasRange { get; init; }

    [JsonIgnore]
    public Memory<byte>? Data { get; set; }

    public static PackedBitVector Default => new(0, 0);

    private bool ShouldDeserializeData => DataStart > -1 && Data == null;

    [JsonIgnore]
    public bool ShouldDeserialize => ShouldDeserializeData;

    public static PackedBitVector FromReader(BiEndianBinaryReader reader, SerializedFile file, bool hasRange) {
        var count = reader.ReadUInt32();
        var range = 0f;
        var start = 0f;
        if (hasRange) {
            range = reader.ReadSingle();
            start = reader.ReadSingle();
        }

        var dataStart = reader.BaseStream.Position;
        var dataCount = reader.ReadInt32();
        reader.BaseStream.Seek(dataCount, SeekOrigin.Current);
        reader.Align();
        var bitSize = reader.ReadByte();
        reader.Align();
        return new PackedBitVector(count, bitSize) { Range = range, Start = start, DataStart = dataStart, HasRange = hasRange };
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        if (ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        writer.Write(Count);
        if (HasRange) {
            writer.Write(Range);
            writer.Write(Start);
        }

        writer.WriteMemory(Data);
        writer.Align();
        writer.Write(BitSize);
    }

    public void Deserialize(BiEndianBinaryReader reader, SerializedFile serializedFile, ObjectDeserializationOptions options) {
        if (ShouldDeserializeData) {
            reader.BaseStream.Seek(DataStart, SeekOrigin.Begin);
            var dataCount = reader.ReadInt32();
            Data = reader.ReadMemory(dataCount);
        }
    }

    // code taken from AssetStudio, with some edits
    public Memory<int> Decompress(uint? count = null, int offset = 0) {
        count ??= Count;
        if (count > Count) {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (offset < 0 || count + offset > Count) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (count == 0) {
            return Memory<int>.Empty;
        }

        var data = new int[count.Value];
        var bitPos = BitSize * offset;
        var indexPos = bitPos / 8;
        bitPos %= 8;
        var memory = Data!.Value.Span;
        var index = 0;
        for (var i = offset; i < count + offset; i++) {
            var bits = 0;
            var currentIndex = index++;
            data[currentIndex] = 0;
            while (bits < BitSize) {
                data[currentIndex] |= (memory[indexPos] >> bitPos) << bits;

                var num = System.Math.Min(BitSize - bits, 8 - bitPos);
                bitPos += num;
                bits += num;

                if (bitPos == 8) {
                    indexPos++;
                    bitPos = 0;
                }
            }

            data[currentIndex] &= (1 << BitSize) - 1;
        }

        return data;
    }

    // same as Decompress
    public Memory<float> DecompressSingle(uint? count = null, int offset = 0) {
        count ??= Count;
        if (count > Count) {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (offset < 0 || count + offset > Count) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (count == 0) {
            return Memory<float>.Empty;
        }

        var data = new float[count.Value];
        var bitPos = BitSize * offset;
        var indexPos = bitPos / 8;
        bitPos %= 8;
        var memory = Data!.Value.Span;
        var scale = 1.0f / Range;
        var index = 0;
        for (var i = offset; i < count + offset; i++) {
            var x = 0u;
            var bits = 0;
            while (bits < BitSize) {
                x |= (uint) ((memory[indexPos] >> bitPos) << bits);
                var num = System.Math.Min(BitSize - bits, 8 - bitPos);
                bitPos += num;
                bits += num;
                if (bitPos == 8) {
                    indexPos++;
                    bitPos = 0;
                }
            }

            x &= (uint) (1 << BitSize) - 1u;
            data[index++] = x / (scale * ((1 << BitSize) - 1)) + Start;
        }

        return data;
    }

    public void Free() {
        Data = Memory<byte>.Empty;
    }
}
