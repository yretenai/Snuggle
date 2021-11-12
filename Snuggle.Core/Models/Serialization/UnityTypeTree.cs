using System;
using System.Text;
using JetBrains.Annotations;
using Snuggle.Core.Extensions;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Serialization; 

[PublicAPI]
public record UnityTypeTree(
    UnityTypeTreeNode[] Nodes,
    Memory<byte> StringBuffer) {
    public static UnityTypeTree FromReader(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleOptions options) => header.FileVersion is >= UnitySerializedFileVersion.TypeTreeBlob or UnitySerializedFileVersion.TypeTreeBlobBeta ? FromReaderBlob(reader, header, options) : FromReaderLegacy(reader, header, options);

    private static UnityTypeTree FromReaderLegacy(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleOptions options) => new(UnityTypeTreeNode.ArrayFromReaderLegacy(reader, header, options, 1, 0), Memory<byte>.Empty);

    private static UnityTypeTree FromReaderBlob(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleOptions options) {
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

    public string PrintLayout(bool fullInfo, bool skipIgnored) {
        var sb = new StringBuilder();
        foreach (var node in Nodes) {
            if (skipIgnored && node.Flags.HasFlag(UnityTypeTreeFlags.Ignored)) {
                continue;
            }

            sb.Append(' ', node.Level * 2);
            sb.Append($"{node.Type} {node.Name}");
            if (fullInfo) {
                sb.Append($"; Size: {node.Size}; Array Type: {node.Flags:G}; Flags: {node.Flags.ToFlagString()}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}