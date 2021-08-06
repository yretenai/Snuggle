using System;
using System.IO;
using System.Text.Json.Serialization;
using Equilibrium.Exceptions;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record PackedBitVector(
        uint Count,
        byte BitSize) {
        private long DataStart { get; init; } = -1;
        public float Range { get; init; }
        public float Start { get; init; }
        public bool HasRange { get; init; }

        [JsonIgnore]
        public Memory<byte>? Data { get; set; }

        public static PackedBitVector Default { get; } = new(0, 0);

        [JsonIgnore]
        public bool ShouldDeserialize => Data == null;

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
                throw new IncompleteDeserializationException();
            }

            writer.Write(Count);
            if (HasRange) {
                writer.Write(Range);
                writer.Write(Start);
            }

            writer.Write(Data!.Value.Length);
            writer.WriteMemory(Data);
            writer.Align();
            writer.Write(BitSize);
        }

        public void Deserialize(BiEndianBinaryReader reader, SerializedFile serializedFile, ObjectDeserializationOptions options) {
            reader.BaseStream.Seek(DataStart, SeekOrigin.Begin);
            var dataCount = reader.ReadInt32();
            Data = reader.ReadMemory(dataCount);
        }
    }
}
