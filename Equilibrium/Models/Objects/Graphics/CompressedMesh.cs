using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record CompressedMesh(
        PackedBitVector Vertices,
        PackedBitVector UVs,
        PackedBitVector Normals,
        PackedBitVector Tangents,
        PackedBitVector Weights,
        PackedBitVector NormalSigns,
        PackedBitVector TangentSigns,
        PackedBitVector FloatColors,
        PackedBitVector BoneIndices,
        PackedBitVector Triangles,
        uint UVInfo) {
        public static CompressedMesh Default { get; } = new(
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            0);

        [JsonIgnore]
        public bool ShouldDeserialize =>
            Vertices.ShouldDeserialize ||
            UVs.ShouldDeserialize ||
            Normals.ShouldDeserialize ||
            Tangents.ShouldDeserialize ||
            Weights.ShouldDeserialize ||
            NormalSigns.ShouldDeserialize ||
            TangentSigns.ShouldDeserialize ||
            FloatColors.ShouldDeserialize ||
            BoneIndices.ShouldDeserialize ||
            Triangles.ShouldDeserialize;

        public static CompressedMesh FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var vertices = PackedBitVector.FromReader(reader, file, true);
            var uvs = PackedBitVector.FromReader(reader, file, true);
            var normals = PackedBitVector.FromReader(reader, file, true);
            var tangents = PackedBitVector.FromReader(reader, file, true);
            var weights = PackedBitVector.FromReader(reader, file, false);
            var normalSigns = PackedBitVector.FromReader(reader, file, false);
            var tangentSigns = PackedBitVector.FromReader(reader, file, false);
            var floatColors = PackedBitVector.FromReader(reader, file, true);
            var boneIndices = PackedBitVector.FromReader(reader, file, false);
            var triangles = PackedBitVector.FromReader(reader, file, false);
            var uvInfo = reader.ReadUInt32();
            return new CompressedMesh(vertices, uvs, normals, tangents, weights, normalSigns, tangentSigns, floatColors, boneIndices, triangles, uvInfo);
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            Vertices.ToWriter(writer, serializedFile, targetVersion);
            UVs.ToWriter(writer, serializedFile, targetVersion);
            Normals.ToWriter(writer, serializedFile, targetVersion);
            Tangents.ToWriter(writer, serializedFile, targetVersion);
            Weights.ToWriter(writer, serializedFile, targetVersion);
            NormalSigns.ToWriter(writer, serializedFile, targetVersion);
            TangentSigns.ToWriter(writer, serializedFile, targetVersion);
            FloatColors.ToWriter(writer, serializedFile, targetVersion);
            BoneIndices.ToWriter(writer, serializedFile, targetVersion);
            Triangles.ToWriter(writer, serializedFile, targetVersion);
            writer.Write(UVInfo);
        }

        public void Deserialize(BiEndianBinaryReader reader, SerializedFile serializedFile, ObjectDeserializationOptions options) {
            if (Vertices.ShouldDeserialize) {
                Vertices.Deserialize(reader, serializedFile, options);
            }

            if (UVs.ShouldDeserialize) {
                UVs.Deserialize(reader, serializedFile, options);
            }

            if (Normals.ShouldDeserialize) {
                Normals.Deserialize(reader, serializedFile, options);
            }

            if (Tangents.ShouldDeserialize) {
                Tangents.Deserialize(reader, serializedFile, options);
            }

            if (Weights.ShouldDeserialize) {
                Weights.Deserialize(reader, serializedFile, options);
            }

            if (NormalSigns.ShouldDeserialize) {
                NormalSigns.Deserialize(reader, serializedFile, options);
            }

            if (TangentSigns.ShouldDeserialize) {
                TangentSigns.Deserialize(reader, serializedFile, options);
            }

            if (FloatColors.ShouldDeserialize) {
                FloatColors.Deserialize(reader, serializedFile, options);
            }

            if (BoneIndices.ShouldDeserialize) {
                BoneIndices.Deserialize(reader, serializedFile, options);
            }

            if (Triangles.ShouldDeserialize) {
                Triangles.Deserialize(reader, serializedFile, options);
            }
        }

        public (PackedBitVector Vector, object? Meta) GetVectorForChannel(VertexChannel channel) {
            switch (channel) {
                case VertexChannel.Vertex:
                    return (Vertices, null);
                case VertexChannel.Normal:
                    return (Normals, NormalSigns);
                case VertexChannel.Tangent:
                    return (Tangents, TangentSigns);
                case VertexChannel.Color:
                    return (FloatColors, null);
                case VertexChannel.UV0:
                case VertexChannel.UV1:
                case VertexChannel.UV2:
                case VertexChannel.UV3:
                case VertexChannel.UV4:
                case VertexChannel.UV5:
                case VertexChannel.UV6:
                case VertexChannel.UV7:
                    return (UVs, GetUVInfo(channel - VertexChannel.UV0));
                case VertexChannel.SkinWeight:
                    return (Weights, 31f);
                case VertexChannel.SkinBoneIndex:
                    return (BoneIndices, null);
                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
            }
        }

        private VertexDimension GetUVInfo(int uvLayer) {
            if (uvLayer > 7) {
                throw new ArgumentOutOfRangeException(nameof(uvLayer));
            }

            var bits = (UVInfo >> (uvLayer * 4)) & 15;
            var dimension = (VertexDimension) (1 + (bits & 3));
            var enabled = (bits & 4) != 0;

            return enabled ? dimension : VertexDimension.None;
        }

        public Memory<byte> Decompress(uint vertexCount, out Dictionary<VertexChannel, ChannelInfo> channels) {
            channels = new Dictionary<VertexChannel, ChannelInfo>();
            {
                var offset = 0;
                foreach (var channel in Enum.GetValues<VertexChannel>()) {
                    var (vector, meta) = GetVectorForChannel(channel);
                    if (vector.Count == 0) {
                        channels[channel] = ChannelInfo.Default;
                        continue;
                    }

                    ChannelInfo channelInfo;
                    switch (channel) {
                        case VertexChannel.Vertex:
                        case VertexChannel.Tangent:
                        case VertexChannel.Normal:
                            channelInfo = new ChannelInfo(0, offset, VertexFormat.Single, VertexDimension.RGB, 0);
                            break;
                        case VertexChannel.Color:
                            channelInfo = new ChannelInfo(0, offset, VertexFormat.Color, VertexDimension.RGBA, 0);
                            break;
                        case VertexChannel.UV0:
                        case VertexChannel.UV1:
                        case VertexChannel.UV2:
                        case VertexChannel.UV3:
                        case VertexChannel.UV4:
                        case VertexChannel.UV5:
                        case VertexChannel.UV6:
                        case VertexChannel.UV7:
                            channelInfo = new ChannelInfo(0, offset, VertexFormat.Single, (VertexDimension) (meta ?? throw new InvalidOperationException()), 0);
                            break;
                        case VertexChannel.SkinWeight:
                            channelInfo = new ChannelInfo(0, offset, VertexFormat.Single, VertexDimension.RGBA, 0);
                            break;
                        case VertexChannel.SkinBoneIndex:
                            channelInfo = new ChannelInfo(0, offset, VertexFormat.SInt32, VertexDimension.RGBA, 0);
                            break;
                        default:
                            continue;
                    }

                    channels[channel] = channelInfo;
                    offset += channelInfo.GetSize();
                }
            }

            Memory<byte> data = new byte[channels.Sum(x => x.Value.GetSize()) * vertexCount];

            var substreams = new Dictionary<VertexChannel, Memory<byte>>();
            // TODO: loop over each channel and decompress substream.
            // TODO: loop over each vertex and reconstruct VBO.

            return data;
        }
    }
}
