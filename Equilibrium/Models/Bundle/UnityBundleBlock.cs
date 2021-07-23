using System;
using System.Collections.Generic;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public record UnityBundleBlock(
        long Offset,
        long Size,
        uint Flags,
        string Path) {
        public static UnityBundleBlock FromReader(BiEndianBinaryReader reader) =>
            new(
                reader.ReadInt64(),
                reader.ReadInt64(),
                reader.ReadUInt32(),
                reader.ReadNullString()
            );

        public static ICollection<UnityBundleBlock> ArrayFromReader(BiEndianBinaryReader reader,
            UnityBundle header,
            int count) {
            switch (header.Format) {
                case UnityFormat.FS: {
                    var container = new List<UnityBundleBlock>(count);
                    for (var i = 0; i < count; ++i) {
                        container.Add(FromReader(reader));
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
