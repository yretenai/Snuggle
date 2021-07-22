using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI, StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UnityBundleBlockInfo : IReversibleStruct {
        public int Size { get; set; }
        public int CompressedSize { get; set; }
        public UnityBundleBlockFlags Flags { get; set; }

        public static UnityBundleBlockInfo FromReader(BiEndianBinaryReader reader) => reader.ReadStruct<UnityBundleBlockInfo>();

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

        public void ReverseEndianness() {
            CompressedSize = BinaryPrimitives.ReverseEndianness(CompressedSize);
            Size = BinaryPrimitives.ReverseEndianness(Size);
            Flags = (UnityBundleBlockFlags) BinaryPrimitives.ReverseEndianness((ushort) Flags);
        }
    }
}
