using System;
using System.Runtime.InteropServices;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI, StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record UnityBundleBlockInfo(
        int Size,
        int CompressedSize,
        UnityBundleBlockInfoFlags Flags) {
        public static UnityBundleBlockInfo FromReader(BiEndianBinaryReader reader) => new(reader.ReadInt32(), reader.ReadInt32(), (UnityBundleBlockInfoFlags) reader.ReadInt16());

        public static UnityBundleBlockInfo[] ArrayFromReader(BiEndianBinaryReader reader, UnityBundle header, int count) {
            var container = new UnityBundleBlockInfo[count];
            switch (header.Format) {
                case UnityFormat.FS: {
                    for (var i = 0; i < count; ++i) {
                        container[i] = FromReader(reader);
                    }
                }
                    break;
                case UnityFormat.Raw:
                case UnityFormat.Web:
                case UnityFormat.Archive:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException();
            }

            return container;
        }
    }
}
