using System;
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
    }
}
