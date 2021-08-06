using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
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
            reader.BaseStream.Seek(64 * bindPoseCount, SeekOrigin.Current);

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
                reader.BaseStream.Seek(4 * variableBoneCountWeightsCount, SeekOrigin.Current);
            } else {
                BonesAABB = new List<AABB>();
                VariableBoneCountWeights = Memory<uint>.Empty;
            }

            MeshCompression = reader.ReadByte();
            IsReadable = reader.ReadBoolean();
            KeepVertices = reader.ReadBoolean();
            KeepIndices = reader.ReadBoolean();

            reader.Align();

            if (serializedFile.Version >= UnityVersionRegister.Unity2017_4 ||
                serializedFile.Version == UnityVersionRegister.Unity2017_3_1_P ||
                serializedFile.Version >= UnityVersionRegister.Unity2017_3 && MeshCompression == 0) {
                IndexFormat = reader.ReadInt32();
            }

            IndicesStart = reader.BaseStream.Position;
            var indicesCount = reader.ReadInt32();
            reader.BaseStream.Seek(indicesCount, SeekOrigin.Current);
            reader.Align();

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
            reader.BaseStream.Seek(bakedMeshCollisionMeshSize, SeekOrigin.Current);
            reader.Align();

            BakedTriangleCollisionMeshStart = reader.BaseStream.Position;
            var bakedTriangleCollisionMeshSize = reader.ReadInt32();
            reader.BaseStream.Seek(bakedTriangleCollisionMeshSize, SeekOrigin.Current);
            reader.Align();

            if (serializedFile.Version >= UnityVersionRegister.Unity2018_2) {
                MeshMetrics = new[] { reader.ReadSingle(), reader.ReadSingle() };
            } else {
                MeshMetrics = new[] { 0f, 0f };
            }

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

        private long BindPoseStart { get; init; } = -1;
        private long VariableBoneCountWeightsStart { get; init; } = -1;
        private long IndicesStart { get; init; } = -1;
        private long SkinStart { get; init; } = -1;
        private long BakedConvexCollisionMeshStart { get; init; } = -1;
        private long BakedTriangleCollisionMeshStart { get; init; } = -1;

        public List<Submesh> Submeshes { get; set; }
        public BlendShapeData BlendShapeData { get; set; }

        [JsonIgnore]
        public Memory<Matrix4X4>? BindPose { get; set; }

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

        [JsonIgnore]
        public List<BoneWeight>? Skin { get; set; }

        public VertexData VertexData { get; set; }
        public CompressedMesh CompressedMesh { get; set; }
        public AABB LocalAABB { get; set; }
        public int MeshUsageFlags { get; set; }

        [JsonIgnore]
        public Memory<byte>? BakedConvexCollisionMesh { get; set; }

        [JsonIgnore]
        public Memory<byte>? BakedTriangleCollisionMesh { get; set; }

        public float[] MeshMetrics { get; set; }

        [JsonIgnore]
        public override bool ShouldDeserialize =>
            base.ShouldDeserialize ||
            BindPose == null ||
            VariableBoneCountWeights == null ||
            Indices == null ||
            Skin == null ||
            BakedConvexCollisionMesh == null ||
            BakedTriangleCollisionMesh == null ||
            VertexData.ShouldDeserialize ||
            CompressedMesh.ShouldDeserialize ||
            BlendShapeData.ShouldDeserialize;

        public StreamingInfo StreamData { get; set; }

        public override void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
            base.Deserialize(reader, options);
            BlendShapeData.Deserialize(reader, SerializedFile, options);

            if (BindPoseStart > -1) {
                reader.BaseStream.Seek(BindPoseStart, SeekOrigin.Begin);
                var bindPoseCount = reader.ReadInt32();
                BindPose = reader.ReadMemory<Matrix4X4>(bindPoseCount);
            } else {
                BindPose = Memory<Matrix4X4>.Empty;
            }

            if (VariableBoneCountWeightsStart > -1) {
                reader.BaseStream.Seek(VariableBoneCountWeightsStart, SeekOrigin.Begin);
                var variableBoneCountWeightsCount = reader.ReadInt32();
                VariableBoneCountWeights = reader.ReadMemory<uint>(variableBoneCountWeightsCount);
            } else {
                VariableBoneCountWeights = Memory<uint>.Empty;
            }

            if (IndicesStart > -1) {
                reader.BaseStream.Seek(IndicesStart, SeekOrigin.Begin);
                var indicesCount = reader.ReadInt32();
                Indices = reader.ReadMemory(indicesCount);
            } else {
                Indices = Memory<byte>.Empty;
            }

            if (SkinStart > -1) {
                reader.BaseStream.Seek(SkinStart, SeekOrigin.Begin);
                var boneWeightsCount = reader.ReadInt32();
                Skin = new List<BoneWeight>();
                Skin.EnsureCapacity(boneWeightsCount);
                for (var i = 0; i < boneWeightsCount; ++i) {
                    Skin.Add(BoneWeight.FromReader(reader, SerializedFile));
                }
            } else {
                Skin = new List<BoneWeight>();
            }

            VertexData.Deserialize(reader, SerializedFile, options);

            if (VertexData.Data!.Value.IsEmpty &&
                StreamData.Size > 0) {
                VertexData.Data = StreamData.GetData(SerializedFile.Assets, options);
            }

            CompressedMesh.Deserialize(reader, SerializedFile, options);

            if (BakedConvexCollisionMeshStart > -1) {
                reader.BaseStream.Seek(BakedConvexCollisionMeshStart, SeekOrigin.Begin);
                var bakedMeshCollisionMeshSize = reader.ReadInt32();
                BakedConvexCollisionMesh = reader.ReadMemory(bakedMeshCollisionMeshSize);
            }

            if (BakedTriangleCollisionMeshStart > -1) {
                reader.BaseStream.Seek(BakedTriangleCollisionMeshStart, SeekOrigin.Begin);
                var bakedTriangleCollisionMeshSize = reader.ReadInt32();
                BakedTriangleCollisionMesh = reader.ReadMemory(bakedTriangleCollisionMeshSize);
            }
        }

        public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
            throw new InvalidOperationException("Use Serialize(BiEndianBinaryWriter writer, BiEndianBinaryWriter resourceStream, AssetSerializationOptions options)");
        }

        public void Serialize(BiEndianBinaryWriter writer, BiEndianBinaryWriter resourceStream, AssetSerializationOptions options) {
            throw new NotImplementedException();
        }
    }
}
