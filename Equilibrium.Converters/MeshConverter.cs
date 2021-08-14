using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DragonLib;
using Equilibrium.Exceptions;
using Equilibrium.Implementations;
using Equilibrium.Models.Objects.Graphics;
using Equilibrium.Models.Objects.Math;
using JetBrains.Annotations;

namespace Equilibrium.Converters {
    [PublicAPI]
    public static class MeshConverter {
        public static List<StrideInfo> GetStrides(Mesh mesh, Dictionary<VertexChannel, ChannelInfo> descriptors) {
            var strideInfos = new List<StrideInfo>();
            var streamCount = descriptors.Max(x => x.Value.Stream) + 1;
            strideInfos.EnsureCapacity(streamCount);

            var offset = 0;
            for (var i = 0; i < streamCount; ++i) {
                var channels = descriptors.Values.Where(x => x.Stream == i && x.Dimension != VertexDimension.None).ToArray();
                var last = channels.Max(x => x.Offset);
                var lastInfo = channels.First(x => x.Offset == last);
                var stride = last + lastInfo.GetSize();
                strideInfos.Add(new StrideInfo(offset, stride));
                offset = (int) (offset + mesh.VertexData.VertexCount * stride).Align(16);
            }

            return strideInfos;
        }

        private static Dictionary<int, int> CreateBoneHierarchy(Mesh mesh, Renderer? renderer) {
            var bones = new Dictionary<int, int>();
            if (renderer == null) {
                return bones;
            }

            if (renderer is not SkinnedMeshRenderer skinnedMeshRenderer) {
                return bones;
            }

            var transformToBoneHash = new Dictionary<long, uint>();
            var hashToIndex = new List<uint>();

            foreach (var childPtr in skinnedMeshRenderer.Bones) {
                var child = childPtr.Value;
                if (child == null) {
                    continue;
                }

                var gameObject = child.GameObject.Value;
                if (gameObject == null) {
                    continue;
                }

                var crc = new CRC();
                var bytes = Encoding.UTF8.GetBytes(gameObject.Name);
                crc.Update(bytes, 0, (uint) bytes.Length);
                var hash = crc.GetDigest();
                transformToBoneHash[child.PathId] = hash;
                hashToIndex.Add(hash);
            }

            for (var index = 0; index < skinnedMeshRenderer.Bones.Count; index++) {
                var childPtr = skinnedMeshRenderer.Bones[index];
                var child = childPtr.Value;
                if (child == null) {
                    continue;
                }

                var gameObject = child.GameObject.Value;
                if (gameObject == null) {
                    continue;
                }

                if (child.PathId != skinnedMeshRenderer.RootBone.PathId) {
                    bones[index] = hashToIndex.IndexOf(transformToBoneHash[child.Parent.PathId]);
                } else {
                    bones[index] = -1;
                }
            }

            return bones;
        }

        public static Memory<byte> GetVBO(Mesh mesh, out Dictionary<VertexChannel, ChannelInfo> channels) {
            if (mesh.ShouldDeserialize) {
                throw new IncompleteDeserializationException();
            }

            if (mesh.MeshCompression == 0) {
                channels = mesh.VertexData.Channels;
                return mesh.VertexData.Data!.Value;
            }

            return mesh.CompressedMesh.Decompress(mesh.VertexData.VertexCount, out channels);
        }

        public static Memory<byte> GetIBO(Mesh mesh) {
            if (mesh.ShouldDeserialize) {
                throw new IncompleteDeserializationException();
            }

            if (mesh.MeshCompression == 0) {
                return mesh.Indices!.Value;
            }

            mesh.IndexFormat = IndexFormat.Uint32;

            var triangles = mesh.CompressedMesh.Triangles.Decompress();
            return MemoryMarshal.Cast<int, byte>(triangles).ToArray();
        }

        public readonly record struct StrideInfo(int Offset, int Stride);
    }
}
