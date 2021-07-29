using System;
using Equilibrium.IO;
using Equilibrium.Meta.Options;
using JetBrains.Annotations;

namespace Equilibrium.Models.Serialization {
    [PublicAPI]
    public record UnityTypeTree(
        UnityTypeTreeNode[] Nodes,
        Memory<byte> StringBuffer) {
        public static UnityTypeTree FromReader(BiEndianBinaryReader reader, UnitySerializedFile header, EquilibriumOptions options) => header.Version is >= UnitySerializedFileVersion.TypeTreeBlob or UnitySerializedFileVersion.TypeTreeBlobBeta ? FromReaderBlob(reader, header, options) : FromReaderLegacy(reader, header, options);

        private static UnityTypeTree FromReaderLegacy(BiEndianBinaryReader reader, UnitySerializedFile header, EquilibriumOptions options) => new(UnityTypeTreeNode.ArrayFromReaderLegacy(reader, header, options, 1, 0), Memory<byte>.Empty);

        private static UnityTypeTree FromReaderBlob(BiEndianBinaryReader reader, UnitySerializedFile header, EquilibriumOptions options) {
            var nodeCount = reader.ReadInt32();
            var bufferSize = reader.ReadInt32();
            var nodes = new UnityTypeTreeNode[nodeCount];
            for (var i = 0; i < nodeCount; ++i) {
                nodes[i] = UnityTypeTreeNode.FromReader(reader, header, options);
            }

            Memory<byte> buffer = reader.ReadBytes(bufferSize);

            var staticBuffer = UnityTypeTreeNode.StaticBuffer.Span;
            for (var i = 0; i < nodeCount; ++i) {
                var node = nodes[i];
                nodes[i] = node with {
                    Type = UnityTypeTreeNode.GetString(node.TypeOffset, (node.TypeOffset & 0x80000000) == 0 ? buffer.Span : staticBuffer),
                    Name = UnityTypeTreeNode.GetString(node.NameOffset, (node.NameOffset & 0x80000000) == 0 ? buffer.Span : staticBuffer),
                };
            }

            return new UnityTypeTree(nodes, buffer);
        }
    }
}
