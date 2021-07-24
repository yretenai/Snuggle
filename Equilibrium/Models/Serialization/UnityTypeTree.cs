using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Equilibrium.IO;

namespace Equilibrium.Models.Serialization {
    public record UnityTypeTree(
        ImmutableArray<UnityTypeTreeNode> Nodes,
        Memory<byte> StringBuffer) {
        public static UnityTypeTree FromReader(BiEndianBinaryReader reader, UnitySerializedFile header, bool isRef) {
            return header.Version is >= UnitySerializedFileVersion.TypeTreeBlob or UnitySerializedFileVersion.TypeTreeBlobBeta ? FromReaderBlob(reader, header) : FromReaderLegacy(reader, header);
        }

        private static UnityTypeTree FromReaderLegacy(BiEndianBinaryReader reader, UnitySerializedFile header) {
            return new(UnityTypeTreeNode.ArrayFromReaderLegacy(reader, header, 1, 0).ToImmutableArray(), Memory<byte>.Empty);
        }

        private static UnityTypeTree FromReaderBlob(BiEndianBinaryReader reader, UnitySerializedFile header) {
            var nodeCount = reader.ReadInt32();
            var bufferSize = reader.ReadInt32();
            var nodes = new List<UnityTypeTreeNode>(nodeCount);
            for (var i = 0; i < nodeCount; ++i) {
                nodes.Add(UnityTypeTreeNode.FromReader(reader, header));
            }

            Memory<byte> buffer = reader.ReadBytes(bufferSize);

            var staticBuffer = UnityTypeTreeNode.StaticBuffer;
            for (var i = 0; i < nodeCount; ++i) {
                var node = nodes[i];
                nodes[i] = node with {
                    Type = UnityTypeTreeNode.GetString(node.TypeOffset, (node.TypeOffset & 0x80000000) == 0 ? buffer.Span : staticBuffer),
                    Name = UnityTypeTreeNode.GetString(node.NameOffset, (node.NameOffset & 0x80000000) == 0 ? buffer.Span : staticBuffer),
                };
            }

            return new UnityTypeTree(nodes.ToImmutableArray(), buffer);
        }
    }
}
