using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI, StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record UnityBundleBlockInfo(int Size, int CompressedSize, UnityBundleBlockFlags Flags) {
        public static UnityBundleBlockInfo FromReader(BiEndianBinaryReader reader) {
            return new(reader.ReadInt32(), reader.ReadInt32(), (UnityBundleBlockFlags) reader.ReadInt16());
        }

        public static ICollection<UnityBundleBlockInfo> ArrayFromReader(BiEndianBinaryReader reader, UnityBundle header, int count) {
            var container = new List<UnityBundleBlockInfo>(count);
            switch (header.Format) {
                case UnityFormat.FS: {
                    for (var i = 0; i < count; ++i) {
                        container.Add(FromReader(reader));
                    }
                }
                    break;
                case UnityFormat.Raw:
                case UnityFormat.Web:
                case UnityFormat.Archive:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }

            return container;
        }
    }
}
