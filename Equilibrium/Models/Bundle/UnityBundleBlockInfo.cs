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
        public static UnityBundleBlockInfo FromReader(BiEndianBinaryReader reader, UnityBundle header) {
            var size = reader.ReadInt32();
            var compressedSize = reader.ReadInt32();
            var flags = header.Format switch {
                UnityFormat.FS => (UnityBundleBlockInfoFlags) reader.ReadInt16(),
                UnityFormat.Raw => (UnityBundleBlockInfoFlags) 0,
                UnityFormat.Web => (UnityBundleBlockInfoFlags) 1,
                _ => (UnityBundleBlockInfoFlags) 0,
            };
            return new UnityBundleBlockInfo(size, compressedSize, flags);
        }

        public static UnityBundleBlockInfo[] ArrayFromReader(BiEndianBinaryReader reader, UnityBundle header, int count) {
            var container = new UnityBundleBlockInfo[count];
            switch (header.Format) {
                case UnityFormat.FS:
                case UnityFormat.Raw:
                case UnityFormat.Web: {
                    for (var i = 0; i < count; ++i) {
                        container[i] = FromReader(reader, header);
                    }
                }
                    break;
                case UnityFormat.Archive:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException();
            }

            return container;
        }
    }
}
