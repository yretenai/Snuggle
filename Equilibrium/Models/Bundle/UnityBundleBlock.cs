﻿using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public record UnityBundleBlock(
        long Offset,
        long Size,
        UnityBundleBlockFlags Flags,
        string Path) {
        public static UnityBundleBlock FromReader(BiEndianBinaryReader reader, EquilibriumOptions options) =>
            new(
                reader.ReadInt64(),
                reader.ReadInt64(),
                (UnityBundleBlockFlags) reader.ReadUInt32(),
                reader.ReadNullString()
            );

        public static UnityBundleBlock FromReaderRaw(BiEndianBinaryReader reader, EquilibriumOptions options) {
            var path = reader.ReadNullString();
            var offset = reader.ReadUInt32();
            var size = reader.ReadUInt32();
            return new UnityBundleBlock(offset, size, UnityBundleBlockFlags.SerializedFile, path);
        }

        public static UnityBundleBlock[] ArrayFromReader(BiEndianBinaryReader reader,
            UnityBundle header,
            int count,
            EquilibriumOptions options) {
            switch (header.Format) {
                case UnityFormat.FS: {
                    var container = new UnityBundleBlock[count];
                    for (var i = 0; i < count; ++i) {
                        container[i] = FromReader(reader, options);
                    }

                    return container;
                }
                case UnityFormat.Raw:
                case UnityFormat.Web: {
                    var container = new UnityBundleBlock[count];
                    for (var i = 0; i < count; ++i) {
                        container[i] = FromReaderRaw(reader, options);
                    }

                    return container;
                }
                case UnityFormat.Archive:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException();
            }
        }

        public static void ArrayToWriter(BiEndianBinaryWriter writer, UnityBundleBlock[] blocks, UnityBundle header, EquilibriumOptions options, EquilibriumSerializationOptions serializationOptions) {
            writer.Write(blocks.Length);

            var offset = 0L;
            foreach (var block in blocks) {
                block.ToWriter(writer, header, options, serializationOptions, offset);
                offset += block.Size; // Alignment? ModCheck
            }
        }

        private void ToWriter(BiEndianBinaryWriter writer, UnityBundle header, EquilibriumOptions options, EquilibriumSerializationOptions serializationOptions, long offset) {
            if (header.Format == UnityFormat.FS) {
                writer.Write(offset);
                writer.Write(Size);
                writer.Write((uint) Flags);
                writer.WriteNullString(Path);
            } else {
                writer.WriteNullString(Path);
                writer.Write(offset);
                writer.Write(Size);
            }
        }
    }
}
