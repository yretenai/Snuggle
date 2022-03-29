using System;
using System.Collections.Generic;
using System.Linq;
using Snuggle.Core;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Implementations;
using Snuggle.Core.Models.Objects.Graphics;

namespace Snuggle.Converters;

public static class MeshConverter {
    public static Memory<byte>[] GetVBO(Mesh mesh, out uint vertexCount, out Dictionary<VertexChannel, ChannelInfo> channels, out int[] strides) {
        if (mesh.ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        Memory<byte> fullBuffer;
        if (mesh.MeshCompression == 0) {
            vertexCount = mesh.VertexData.VertexCount;
            channels = mesh.VertexData.Channels;
            fullBuffer = mesh.VertexData.Data!.Value;
        } else {
            fullBuffer = mesh.CompressedMesh.Decompress(out vertexCount, out channels);
        }

        return GetVBO(fullBuffer, vertexCount, channels, out strides);
    }

    public static Memory<byte>[] GetVBO(Memory<byte> fullBuffer, uint vertexCount, Dictionary<VertexChannel, ChannelInfo> channels, out int[] strides) {
        var streamCount = channels.Max(x => x.Value.Stream) + 1;
        strides = new int[streamCount];
        var vbos = new Memory<byte>[streamCount];
        var offset = 0;
        for (var index = 0; index < streamCount; index++) {
            var streamChannels = channels.Values.Where(x => x.Stream == index && x.Dimension != VertexDimension.None).ToArray();
            if (streamChannels.Length == 0) {
                continue;
            }

            var last = streamChannels.Max(x => x.Offset);
            var lastInfo = streamChannels.First(x => x.Offset == last);
            strides[index] = last + lastInfo.GetSize();
            var length = vertexCount * strides[index];
            vbos[index] = fullBuffer.Slice(offset, (int) length);
            offset += (int) length;
            if (offset % 16 > 0) {
                offset += 16 - offset % 16;
            }
        }

        return vbos;
    }

    public static Memory<byte> GetIBO(Mesh mesh) {
        if (mesh.ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        if (mesh.MeshCompression == 0) {
            return mesh.Indices!.Value;
        }

        mesh.IndexFormat = IndexFormat.Uint32;

        var triangles = mesh.CompressedMesh.Triangles.Decompress();
        return triangles.AsBytes();
    }
}
