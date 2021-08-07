using System;
using System.IO;
using System.Text.Json.Serialization;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects {
    [PublicAPI]
    public record StreamingInfo(
        long Offset,
        long Size,
        string Path) {
        public static StreamingInfo Null { get; } = new(0, 0, string.Empty);

        [JsonIgnore]
        public bool IsNull => Size == 0 || string.IsNullOrEmpty(Path);

        public static StreamingInfo FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var offset = file.Version >= UnityVersionRegister.Unity2020_1 ? reader.ReadInt64() : reader.ReadUInt32();
            var size = reader.ReadUInt32();
            var path = reader.ReadString32();
            return new StreamingInfo(offset, size, path);
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            if (targetVersion >= UnityVersionRegister.Unity2020_1) {
                writer.Write(Offset);
            } else {
                writer.Write((uint) Offset);
            }

            writer.Write((uint) Size);
            writer.WriteString32(Path);
        }

        public Memory<byte> GetData(AssetCollection? assets, ObjectDeserializationOptions options, Memory<byte>? existingData = null) {
            var existing = existingData ?? Memory<byte>.Empty;
            if (assets == null) {
                return existing;
            }

            if (!assets.TryOpenResource(Path, out var resourceStream)) {
                return existing;
            }

            try {
                if (resourceStream.Length < Offset + Size) {
                    return existing;
                }

                if (resourceStream.Length < Offset) {
                    return existing;
                }

                resourceStream.Seek(Offset, SeekOrigin.Current);
                Memory<byte> memory = new byte[existing.Length + Size];
                existing.CopyTo(memory);
                resourceStream.Read(memory[existing.Length..].Span);
                return memory;
            } finally {
                resourceStream.Dispose();
            }
        }
    }
}
