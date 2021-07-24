using System;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public record UnityBundleBlock(
        long Offset,
        long Size,
        UnityBundleBlockFlags Flags,
        string Path) {
        public static UnityBundleBlock FromReader(BiEndianBinaryReader reader) =>
            new(
                reader.ReadInt64(),
                reader.ReadInt64(),
                (UnityBundleBlockFlags) reader.ReadUInt32(),
                reader.ReadNullString()
            );

        public static UnityBundleBlock[] ArrayFromReader(BiEndianBinaryReader reader,
            UnityBundle header,
            int count) {
            switch (header.Format) {
                case UnityFormat.FS: {
                    var container = new UnityBundleBlock[count];
                    for (var i = 0; i < count; ++i) {
                        container[i] = FromReader(reader);
                    }

                    return container;
                }
                case UnityFormat.Raw:
                case UnityFormat.Web:
                case UnityFormat.Archive:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
