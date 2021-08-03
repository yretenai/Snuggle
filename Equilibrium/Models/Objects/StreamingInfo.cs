using System;
using System.IO;
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
        public static StreamingInfo Default { get; } = new(0, 0, string.Empty);

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

        public Memory<byte> GetData(AssetCollection? assets, ObjectDeserializationOptions options) {
            if (assets == null) {
                return Memory<byte>.Empty;
            }

            if (!assets.TryOpenResource(Path, out var resourceStream)) {
                return Memory<byte>.Empty;
            }

            try {
                if (resourceStream.Length < Offset + Size) {
                    return Memory<byte>.Empty;
                }

                if (resourceStream.Length < Offset) {
                    return Memory<byte>.Empty;
                }

                resourceStream.Seek(Offset, SeekOrigin.Current);
                Memory<byte> memory = new byte[Size];
                resourceStream.Read(memory.Span);
                return memory;
            } finally {
                resourceStream.Dispose();
            }
        }
    }
}
