using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Snuggle.Core.Exceptions;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Graphics;

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
    public bool ShouldDeserialize => Vertices.ShouldDeserialize || UVs.ShouldDeserialize || Normals.ShouldDeserialize || Tangents.ShouldDeserialize || Weights.ShouldDeserialize || NormalSigns.ShouldDeserialize || TangentSigns.ShouldDeserialize || FloatColors.ShouldDeserialize || BoneIndices.ShouldDeserialize || Triangles.ShouldDeserialize;

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
        return new CompressedMesh(
            vertices,
            uvs,
            normals,
            tangents,
            weights,
            normalSigns,
            tangentSigns,
            floatColors,
            boneIndices,
            triangles,
            uvInfo);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        if (ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

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

    public PackedBitVector GetVectorForChannel(VertexChannel channel) {
        switch (channel) {
            case VertexChannel.Vertex:
                return Vertices;
            case VertexChannel.Normal:
                return Normals;
            case VertexChannel.Tangent:
                return Tangents;
            case VertexChannel.Color:
                return FloatColors;
            case VertexChannel.UV0:
            case VertexChannel.UV1:
            case VertexChannel.UV2:
            case VertexChannel.UV3:
            case VertexChannel.UV4:
            case VertexChannel.UV5:
            case VertexChannel.UV6:
            case VertexChannel.UV7:
                return UVs;
            case VertexChannel.SkinWeight:
                return Weights;
            case VertexChannel.SkinBoneIndex:
                return BoneIndices;
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

    public Memory<byte> Decompress(out uint vertexCount, out ChannelInfo[] channels) {
        var substreams = new Memory<byte>[(int) VertexChannel.MaxChannelsPlusOne];
        channels = new ChannelInfo[(int) VertexChannel.MaxChannelsPlusOne];
        var offset = 0;
        vertexCount = Vertices.Count / 3;
        if (vertexCount == 0) {
            return Memory<byte>.Empty;
        }

        for (var channel = VertexChannel.Vertex; channel < VertexChannel.MaxChannelsPlusOne; channel++) {
            var vector = GetVectorForChannel(channel);
            if (vector.Count == 0) {
                channels[(int) channel] = ChannelInfo.Default;
                continue;
            }

            var channelInfo = ChannelInfo.Default;
            switch (channel) {
                case VertexChannel.Vertex: {
                    channelInfo = new ChannelInfo(0, offset, VertexFormat.Single, VertexDimension.RGB, 0);
                    substreams[(int) channel] = vector.DecompressSingle().AsBytes();
                    break;
                }
                case VertexChannel.Tangent: {
                    channelInfo = new ChannelInfo(0, offset, VertexFormat.Single, VertexDimension.RGBA, 0);
                    var tangentData = vector.DecompressSingle().Span;
                    var tangentSigns = TangentSigns.Decompress().Span;
                    var tangents = new Memory<float>(new float[vertexCount * 4]);
                    for (var i = 0; i < vertexCount; ++i) {
                        var x = tangentData[i * 2 + 0];
                        var y = tangentData[i * 2 + 1];
                        var zSquared = 1 - x * x - y * y;
                        float z;
                        if (zSquared >= 0f) {
                            z = (float) System.Math.Sqrt(zSquared);
                        } else {
                            z = 0;
                            var tangent = Vector3.Normalize(new Vector3(x, y, z));
                            x = tangent.X;
                            y = tangent.Y;
                            z = tangent.Z;
                        }

                        if (tangentSigns[i * 2 + 0] == 0) {
                            z = -z;
                        }

                        var w = tangentSigns[i * 2 + 1] > 0 ? 1.0f : -1.0f;
                        tangents.Span[i * 4] = x;
                        tangents.Span[i * 4 + 1] = y;
                        tangents.Span[i * 4 + 2] = z;
                        tangents.Span[i * 4 + 3] = w;
                    }

                    substreams[(int) channel] = tangents.AsBytes();
                    break;
                }
                case VertexChannel.Normal: {
                    channelInfo = new ChannelInfo(0, offset, VertexFormat.Single, VertexDimension.RGB, 0);
                    var normalData = vector.DecompressSingle().Span;
                    var normalSigns = NormalSigns.Decompress().Span;
                    var normals = new Memory<float>(new float[vertexCount * 3]);
                    for (var i = 0; i < vertexCount; ++i) {
                        var x = normalData[i * 2 + 0];
                        var y = normalData[i * 2 + 1];
                        var zSquared = 1 - x * x - y * y;
                        float z;
                        if (zSquared >= 0f) {
                            z = (float) System.Math.Sqrt(zSquared);
                        } else {
                            z = 0;
                            var normal = Vector3.Normalize(new Vector3(x, y, z));
                            x = normal.X;
                            y = normal.Y;
                            z = normal.Z;
                        }

                        if (normalSigns[i] == 0) {
                            z = -z;
                        }

                        normals.Span[i * 3] = x;
                        normals.Span[i * 3 + 1] = y;
                        normals.Span[i * 3 + 2] = z;
                    }

                    substreams[(int) channel] = normals.AsBytes();
                    break;
                }
                case VertexChannel.Color: {
                    channelInfo = new ChannelInfo(0, offset, VertexFormat.Color, VertexDimension.RGBA, 0);
                    substreams[(int) channel] = vector.Decompress().AsBytes();
                    break;
                }
                case VertexChannel.UV0:
                case VertexChannel.UV1:
                case VertexChannel.UV2:
                case VertexChannel.UV3:
                case VertexChannel.UV4:
                case VertexChannel.UV5:
                case VertexChannel.UV6:
                case VertexChannel.UV7: {
                    var uvIndex = channel - VertexChannel.UV0;
                    var uvInfo = GetUVInfo(uvIndex);
                    if (uvInfo == VertexDimension.None) {
                        break;
                    }

                    channelInfo = new ChannelInfo(0, offset, VertexFormat.Single, uvInfo, 0);
                    substreams[(int) channel] = vector.DecompressSingle(vertexCount * 2, (int) (uvIndex * 2 * vertexCount)).AsBytes();
                    break;
                }
                case VertexChannel.SkinBoneIndex: {
                    channels[(int) VertexChannel.SkinWeight] = new ChannelInfo(0, offset, VertexFormat.Single, VertexDimension.RGBA, 0);
                    offset += channels[(int) VertexChannel.SkinWeight].GetSize();
                    channelInfo = new ChannelInfo(0, offset, VertexFormat.SInt32, VertexDimension.RGBA, 0);

                    var skinIndices = vector.Decompress().Span;
                    var skinWeights = Weights.Decompress().Span;
                    var normalizedSkinIndices = new Span<int>(new int[vertexCount * 4]);
                    var normalizedSkinWeights = new Span<float>(new float[vertexCount * 4]);
                    var skinIndex = 0;
                    var skinWeight = 0;
                    for (var i = 0; i < vertexCount * 4; i += 4) {
                        var sum = 0;
                        for (var j = 0; j < 4; j++) {
                            normalizedSkinIndices[i + j] = skinIndices[skinIndex++];
                            var weight = j == 3 ? 31 - sum : skinWeights[skinWeight++];
                            normalizedSkinWeights[i + j] = weight / 31.0f;
                            sum += weight;

                            if (sum >= 31) {
                                break;
                            }
                        }
                    }

                    substreams[(int) VertexChannel.SkinWeight] = new Memory<byte>(MemoryMarshal.AsBytes(normalizedSkinWeights).ToArray());
                    substreams[(int) channel] = new Memory<byte>(MemoryMarshal.AsBytes(normalizedSkinIndices).ToArray());
                    break;
                }
                case VertexChannel.SkinWeight: // handled in boneWeight
                default:
                    continue;
            }

            channels[(int) channel] = channelInfo;
            offset += channelInfo.GetSize();
        }

        var stride = offset;
        Memory<byte> data = new byte[stride * vertexCount];
        for (var i = 0; i < vertexCount; ++i) {
            for (var channel = 0; channel < channels.Length; channel++) {
                var info = channels[channel];
                if (info.Dimension == VertexDimension.None) {
                    continue;
                }

                var slice = substreams[channel].Span.Slice(i * info.GetSize(), info.GetSize());
                slice.CopyTo(data.Span[(info.Offset + i * stride)..]);
            }
        }

        return data;
    }

    public void Free() {
        Vertices.Free();
        UVs.Free();
        Normals.Free();
        Tangents.Free();
        Weights.Free();
        NormalSigns.Free();
        TangentSigns.Free();
        FloatColors.Free();
        BoneIndices.Free();
        Triangles.Free();
    }
}
