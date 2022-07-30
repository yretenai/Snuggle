using System;
using System.Text;
using Snuggle.Core.Extensions;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Serialization;

public record UnityTypeTree(UnityTypeTreeNode[] Nodes, Memory<byte> StringBuffer) {
    public static UnityTypeTree FromReader(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleCoreOptions options) => header.FileVersion is >= UnitySerializedFileVersion.TypeTreeBlob or UnitySerializedFileVersion.TypeTreeBlobBeta ? FromReaderBlob(reader, header, options) : FromReaderLegacy(reader, header, options);

    private static UnityTypeTree FromReaderLegacy(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleCoreOptions options) => new(UnityTypeTreeNode.ArrayFromReaderLegacy(reader, header, options, 1, 0), Memory<byte>.Empty);

    private static UnityTypeTree FromReaderBlob(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleCoreOptions options) {
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
            nodes[i] = node with { Type = UnityTypeTreeNode.GetString(node.TypeOffset, (node.TypeOffset & 0x80000000) == 0 ? buffer.Span : staticBuffer), Name = UnityTypeTreeNode.GetString(node.NameOffset, (node.NameOffset & 0x80000000) == 0 ? buffer.Span : staticBuffer) };
        }

        return new UnityTypeTree(nodes, buffer);
    }

    public void ToWriter(BiEndianBinaryWriter writer, UnitySerializedFile header, SnuggleCoreOptions options, AssetSerializationOptions serializationOptions) {
        if (serializationOptions.TargetFileVersion is >= UnitySerializedFileVersion.TypeTreeBlob or UnitySerializedFileVersion.TypeTreeBlobBeta) {
            ToWriterBlob(writer, header, options, serializationOptions);
        } else {
            ToWriterLegacy(writer, header, options, serializationOptions);
        }
    }

    private void ToWriterBlob(BiEndianBinaryWriter writer, UnitySerializedFile header, SnuggleCoreOptions options, AssetSerializationOptions serializationOptions) {
        writer.Write(Nodes.Length);
        writer.Write(StringBuffer.Length);
        foreach (var node in Nodes) {
            node.ToWriter(writer, header, options, serializationOptions);
        }

        writer.Write(StringBuffer.Span);
    }

    private void ToWriterLegacy(BiEndianBinaryWriter writer, UnitySerializedFile header, SnuggleCoreOptions options, AssetSerializationOptions serializationOptions) {
        throw new NotSupportedException("Writing legacy type trees is currently not supported");
    }

    public string PrintLayout(bool fullInfo, bool skipIgnored) {
        var sb = new StringBuilder();
        foreach (var node in Nodes) {
            if (skipIgnored && node.Meta.HasFlag(UnityTransferMetaFlags.IgnoreInMetaFiles)) {
                continue;
            }

            sb.Append(' ', node.Level * 2);
            sb.Append($"{node.Type} {node.Name}");
            if (fullInfo) {
                sb.Append($"; Size: {node.Size}; Array Type: {node.Meta:G}; Flags: {node.Meta.ToFlagString()}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
