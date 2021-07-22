using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public struct UnityBundleBlockInfo {
        public int CompressedSize { get; set; }
        public int Size { get; set; }
        public ushort Flags { get; set; }

        public static UnityBundleBlockInfo FromReader(BiEndianBinaryReader reader) {
            return reader.ReadStruct<UnityBundleBlockInfo>();
        }

        public static ICollection<UnityBundleBlockInfo> ArrayFromReader(BiEndianBinaryReader reader, UnityBundle header, int count) {
            switch (header.Format) {
                case UnityFormat.FS:
                    return reader.ReadArray<UnityBundleBlockInfo>(count).ToArray();
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
