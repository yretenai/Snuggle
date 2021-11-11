using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DragonLib;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Implementations;
using Snuggle.Core.Models.Objects.Graphics;
using JetBrains.Annotations;

namespace Snuggle.Converters {
    [PublicAPI]
    public static class MeshConverter {
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

        public static Memory<byte>[] GetVBO(Mesh mesh, out Dictionary<VertexChannel, ChannelInfo> channels, out int[] strides) {
            if (mesh.ShouldDeserialize) {
                throw new IncompleteDeserializationException();
            }

            Memory<byte> fullBuffer;
            if (mesh.MeshCompression == 0) {
                channels = mesh.VertexData.Channels;
                fullBuffer = mesh.VertexData.Data!.Value;
            } else {
                fullBuffer = mesh.CompressedMesh.Decompress(mesh.VertexData.VertexCount, out channels);
            }

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
                var length = mesh.VertexData.VertexCount * strides[index];
                vbos[index] = fullBuffer.Slice(offset, (int) length);
                offset = (int) (offset + length).Align(16);
            }

            return vbos;
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
    }
}