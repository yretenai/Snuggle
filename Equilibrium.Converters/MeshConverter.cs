using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Equilibrium.Exceptions;
using Equilibrium.Implementations;
using Equilibrium.Models.Objects.Graphics;
using HelixToolkit.SharpDX.Core;
using SharpDX;

namespace Equilibrium.Converters {
    public static class MeshConverter {
        public static List<Object3D> GetSubmeshes(Mesh mesh) {
            if (mesh.ShouldDeserialize) {
                throw new IncompleteDeserializationException();
            }

            var vertexStream = GetVBO(mesh, out var descriptors);
            var indexStream = GetIBO(mesh);

            var objects = new List<Object3D>();
            for (var index = 0; index < mesh.Submeshes.Count; index++) {
                var submesh = mesh.Submeshes[index];
                var geometry = new MeshGeometry3D();
                var span = indexStream.Span.Slice((int) submesh.FirstByte, (int) (submesh.IndexCount * (mesh.IndexFormat == 0 ? 2 : 4)));
                geometry.Indices = new IntCollection(mesh.IndexFormat == 0 ? MemoryMarshal.Cast<byte, short>(span).ToArray().Select(x => (int) x) : MemoryMarshal.Cast<byte, int>(span).ToArray());
                var stride = mesh.VertexData.GetStride();
                var offset = submesh.FirstVertex * stride;
                geometry.Positions = new Vector3Collection();
                geometry.Positions.EnsureCapacity(submesh.VertexCount);
                geometry.Normals = new Vector3Collection();
                geometry.Normals.EnsureCapacity(submesh.VertexCount);
                geometry.Tangents = new Vector3Collection();
                geometry.Tangents.EnsureCapacity(submesh.VertexCount);
                geometry.Colors = new Color4Collection();
                geometry.Colors.EnsureCapacity(submesh.VertexCount);
                geometry.TextureCoordinates = new Vector2Collection();
                geometry.TextureCoordinates.EnsureCapacity(submesh.VertexCount);
                for (var i = 0; i < submesh.VertexCount; ++i) {
                    foreach (var (channel, info) in descriptors) {
                        var data = vertexStream[(offset + info.Offset)..].Span;
                        if (info.Dimension == VertexDimension.None) {
                            continue;
                        }

                        var value = info.Unpack(ref data);
                        switch (channel) {
                            case VertexChannel.Vertex:
                                geometry.Positions.Add(new Vector3(value.Select(x => (float) x).Take(3).ToArray()));
                                break;
                            case VertexChannel.Normal:
                                geometry.Normals.Add(new Vector3(value.Select(x => (float) x).Take(3).ToArray()));
                                break;
                            case VertexChannel.Tangent:
                                geometry.Tangents.Add(new Vector3(value.Select(x => (float) x).Take(3).ToArray()));
                                break;
                            case VertexChannel.Color:
                                geometry.Colors.Add(new Color4((uint) value[0]));
                                break;
                            case VertexChannel.UV0:
                                geometry.TextureCoordinates.Add(new Vector2(value.Select(x => (float) x).Take(2).ToArray()));
                                break;
                            case VertexChannel.UV1:
                            case VertexChannel.UV2:
                            case VertexChannel.UV3:
                            case VertexChannel.UV4:
                            case VertexChannel.UV5:
                            case VertexChannel.UV6:
                            case VertexChannel.UV7:
                            case VertexChannel.SkinWeight:
                            case VertexChannel.SkinBoneIndex:
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }

                    offset += stride;
                }

                var object3D = new Object3D { Name = $"{mesh.Name}_Submesh{index}", Geometry = geometry };
                objects.Add(object3D);
            }

            return objects;
        }

        private static Memory<byte> GetVBO(Mesh mesh, out Dictionary<VertexChannel, ChannelInfo> channels) {
            if (mesh.ShouldDeserialize) {
                throw new IncompleteDeserializationException();
            }

            if (mesh.MeshCompression == 0) {
                channels = mesh.VertexData.Channels;
                return mesh.VertexData.Data!.Value;
            }

            // TODO: Decompress mesh?
            throw new NotImplementedException();
        }

        private static Memory<byte> GetIBO(Mesh mesh) {
            if (mesh.ShouldDeserialize) {
                throw new IncompleteDeserializationException();
            }

            if (mesh.MeshCompression == 0) {
                return mesh.Indices!.Value;
            }

            // TODO: Decompress mesh?
            throw new NotImplementedException();
        }
    }
}
