using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Equilibrium.Interfaces;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Objects;
using Equilibrium.Models.Objects.Graphics;
using Equilibrium.Models.Objects.Math;
using Equilibrium.Models.Serialization;
using Equilibrium.Options;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Equilibrium.Implementations {
    [PublicAPI, ObjectImplementation(UnityClassId.Mesh)]
    public class Mesh : NamedObject, ISerializedResource {
        public Mesh(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            var submeshCount = reader.ReadInt32();
            Submeshes = new List<Submesh>();
            Submeshes.EnsureCapacity(submeshCount);
            for (var i = 0; i < submeshCount; ++i) {
                Submeshes.Add(Submesh.FromReader(reader, serializedFile));
            }

            BlendShapeData = BlendShapeData.FromReader(reader, serializedFile);

            BindPoseStart = reader.BaseStream.Position;
            var bindPoseCount = reader.ReadInt32();
            if (bindPoseCount == 0) {
                BindPose = Memory<Matrix4x4>.Empty;
            } else {
                reader.BaseStream.Seek(64 * bindPoseCount, SeekOrigin.Current);
            }

            var boneNameCount = reader.ReadInt32();
            BoneNameHashes = reader.ReadArray<uint>(boneNameCount).ToArray().ToList();
            RootBoneNameHash = reader.ReadUInt32();

            if (serializedFile.Version >= UnityVersionRegister.Unity2019) {
                var bonesAABBCount = reader.ReadInt32();
                BonesAABB = new List<AABB>();
                BonesAABB.EnsureCapacity(bonesAABBCount);
                for (var i = 0; i < bonesAABBCount; ++i) {
                    BonesAABB.Add(AABB.FromReader(reader, serializedFile));
                }

                VariableBoneCountWeightsStart = reader.BaseStream.Position;
                var variableBoneCountWeightsCount = reader.ReadInt32();
                if (variableBoneCountWeightsCount == 0) {
                    VariableBoneCountWeights = Memory<uint>.Empty;
                } else {
                    reader.BaseStream.Seek(4 * variableBoneCountWeightsCount, SeekOrigin.Current);
                }
            } else {
                BonesAABB = new List<AABB>();
                VariableBoneCountWeights = Memory<uint>.Empty;
            }

            MeshCompression = reader.ReadByte();
            IsReadable = reader.ReadBoolean();
            KeepVertices = reader.ReadBoolean();
            KeepIndices = reader.ReadBoolean();

            if (serializedFile.Version >= UnityVersionRegister.Unity2017_4 ||
                serializedFile.Version == UnityVersionRegister.Unity2017_3_1_P ||
                serializedFile.Version >= UnityVersionRegister.Unity2017_3 && MeshCompression == 0) {
                IndexFormat = reader.ReadInt32();
            }

            IndicesStart = reader.BaseStream.Position;
            var indicesCount = reader.ReadInt32();
            if (indicesCount == 0) {
                Indices = Memory<byte>.Empty;
            } else {
                reader.BaseStream.Seek(indicesCount, SeekOrigin.Current);
            }

            if (serializedFile.Version <= UnityVersionRegister.Unity2018_1) {
                SkinStart = reader.BaseStream.Position;
                var skinCount = reader.ReadInt32();
                if (skinCount != 0) {
                    reader.BaseStream.Seek(skinCount * 4 * 8, SeekOrigin.Current);
                }
            }

            VertexData = VertexData.FromReader(reader, serializedFile);
            CompressedMesh = CompressedMesh.FromReader(reader, serializedFile);
            LocalAABB = AABB.FromReader(reader, serializedFile);
            MeshUsageFlags = reader.ReadInt32();

            BakedConvexCollisionMeshStart = reader.BaseStream.Position;
            var bakedMeshCollisionMeshSize = reader.ReadInt32();
            if (bakedMeshCollisionMeshSize == 0) {
                BakedConvexCollisionMesh = Memory<byte>.Empty;
            } else {
                reader.BaseStream.Seek(bakedMeshCollisionMeshSize, SeekOrigin.Current);
            }

            BakedTriangleCollisionMeshStart = reader.BaseStream.Position;
            var bakedTriangleCollisionMeshSize = reader.ReadInt32();
            if (bakedTriangleCollisionMeshSize == 0) {
                BakedTriangleCollisionMesh = Memory<byte>.Empty;
            } else {
                reader.BaseStream.Seek(bakedTriangleCollisionMeshSize, SeekOrigin.Current);
            }

            if (serializedFile.Version >= UnityVersionRegister.Unity2018_2) {
                MeshMetrics = new[] { reader.ReadSingle(), reader.ReadSingle() };
            } else {
                MeshMetrics = new[] { 0f, 0f };
            }

            reader.Align();

            if (serializedFile.Version >= UnityVersionRegister.Unity2018_3) {
                StreamData = StreamingInfo.FromReader(reader, serializedFile);
            } else {
                StreamData = StreamingInfo.Default;
            }
        }

        public Mesh(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
            Submeshes = new List<Submesh>();
            BlendShapeData = BlendShapeData.Default;
            BoneNameHashes = new List<uint>();
            BonesAABB = new List<AABB>();
            VertexData = VertexData.Default;
            CompressedMesh = CompressedMesh.Default;
            LocalAABB = AABB.Default;
            MeshMetrics = new float[2];
            StreamData = StreamingInfo.Default;
        }

        private long BindPoseStart { get; set; }
        private long VariableBoneCountWeightsStart { get; set; }
        private long IndicesStart { get; set; }
        private long SkinStart { get; set; }
        private long BakedConvexCollisionMeshStart { get; set; }
        private long BakedTriangleCollisionMeshStart { get; set; }

        public List<Submesh> Submeshes { get; set; }
        public BlendShapeData BlendShapeData { get; set; }

        [JsonIgnore]
        public Memory<Matrix4x4>? BindPose { get; set; }

        public List<uint> BoneNameHashes { get; set; }
        public uint RootBoneNameHash { get; set; }
        public List<AABB> BonesAABB { get; set; }

        [JsonIgnore]
        public Memory<uint>? VariableBoneCountWeights { get; set; }

        public byte MeshCompression { get; set; }
        public bool IsReadable { get; set; }
        public bool KeepVertices { get; set; }
        public bool KeepIndices { get; set; }
        public int IndexFormat { get; set; }

        [JsonIgnore]
        public Memory<byte>? Indices { get; set; }

        public VertexData VertexData { get; set; }
        public CompressedMesh CompressedMesh { get; set; }
        public AABB LocalAABB { get; set; }
        public int MeshUsageFlags { get; set; }

        [JsonIgnore]
        public Memory<byte>? BakedConvexCollisionMesh { get; set; }

        [JsonIgnore]
        public Memory<byte>? BakedTriangleCollisionMesh { get; set; }

        public float[] MeshMetrics { get; set; }

        public override bool ShouldDeserialize =>
            base.ShouldDeserialize ||
            BindPose == null ||
            VariableBoneCountWeights == null ||
            Indices == null ||
            BakedConvexCollisionMesh == null ||
            BakedTriangleCollisionMesh == null ||
            VertexData.ShouldDeserialize ||
            CompressedMesh.ShouldDeserialize ||
            BlendShapeData.ShouldDeserialize;

        public StreamingInfo StreamData { get; set; }

        public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
            throw new InvalidOperationException("Use Serialize(BiEndianBinaryWriter writer, BiEndianBinaryWriter resourceStream, AssetSerializationOptions options)");
        }

        public void Serialize(BiEndianBinaryWriter writer, BiEndianBinaryWriter resourceStream, AssetSerializationOptions options) {
            throw new NotImplementedException();
        }
    }
}
