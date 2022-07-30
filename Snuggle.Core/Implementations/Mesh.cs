using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Snuggle.Core.Extensions;
using Snuggle.Core.Game.Unite;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[ObjectImplementation(UnityClassId.Mesh)]
public class Mesh : NamedObject, ISerializedResource {
    public Mesh(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        var submeshCount = reader.ReadInt32();
        Submeshes.EnsureCapacity(submeshCount);
        for (var i = 0; i < submeshCount; ++i) {
            Submeshes.Add(Submesh.FromReader(reader, serializedFile));
        }

        BlendShapeData = BlendShapeData.FromReader(reader, serializedFile);

        BindPoseStart = reader.BaseStream.Position;
        var bindPoseCount = reader.ReadInt32();
        reader.BaseStream.Seek(64 * bindPoseCount, SeekOrigin.Current);

        var boneNameCount = reader.ReadInt32();
        BoneNameHashes = reader.ReadArray<uint>(boneNameCount);
        RootBoneNameHash = reader.ReadUInt32();

        if (serializedFile.Version >= UnityVersionRegister.Unity2019) {
            var bonesAABBCount = reader.ReadInt32();
            BonesAABB.AddRange(reader.ReadSpan<AABB>(bonesAABBCount));

            VariableBoneCountWeightsStart = reader.BaseStream.Position;
            var variableBoneCountWeightsCount = reader.ReadInt32();
            reader.BaseStream.Seek(4 * variableBoneCountWeightsCount, SeekOrigin.Current);
        }

        if (serializedFile.Options.Game is UnityGame.PokemonUnite) {
            var container = GetExtraContainer<UniteMeshExtension>(UnityClassId.Mesh);
            container.BoneCount = reader.ReadInt32();
        }

        MeshCompression = (MeshCompression) reader.ReadByte();
        IsReadable = reader.ReadBoolean();
        KeepVertices = reader.ReadBoolean();
        KeepIndices = reader.ReadBoolean();

        reader.Align();

        if (serializedFile.Version >= UnityVersionRegister.Unity2017_4 || serializedFile.Version == UnityVersionRegister.Unity2017_3_1_P || serializedFile.Version >= UnityVersionRegister.Unity2017_3 && MeshCompression == MeshCompression.Off) {
            IndexFormat = (IndexFormat) reader.ReadInt32();
        }

        IndicesStart = reader.BaseStream.Position;
        var indicesCount = reader.ReadInt32();
        reader.BaseStream.Seek(indicesCount, SeekOrigin.Current);
        reader.Align();

        if (serializedFile.Version < UnityVersionRegister.Unity2018_2) {
            SkinStart = reader.BaseStream.Position;
            var skinCount = reader.ReadInt32();
            if (skinCount != 0) {
                reader.BaseStream.Seek(skinCount * 4 * 8, SeekOrigin.Current);
            }
        }

        VertexData = VertexData.FromReader(reader, serializedFile);
        CompressedMesh = CompressedMesh.FromReader(reader, serializedFile);
        LocalAABB = reader.ReadStruct<AABB>();
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
            StreamDataOffset = reader.BaseStream.Position;
            StreamData = StreamingInfo.FromReader(reader, serializedFile);
        } else {
            StreamDataOffset = -1;
            StreamData = StreamingInfo.Null;
        }
    }

    public Mesh(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
        BlendShapeData = BlendShapeData.Default;
        VertexData = VertexData.Default;
        CompressedMesh = CompressedMesh.Default;
        LocalAABB = AABB.Default;
        MeshMetrics = new float[2];
        StreamDataOffset = -1;
        StreamData = StreamingInfo.Null;
    }

    private long BindPoseStart { get; } = -1;
    private long VariableBoneCountWeightsStart { get; } = -1;
    private long IndicesStart { get; } = -1;
    private long SkinStart { get; } = -1;
    private long BakedConvexCollisionMeshStart { get; } = -1;
    private long BakedTriangleCollisionMeshStart { get; } = -1;

    public List<Submesh> Submeshes { get; set; } = new();
    public BlendShapeData BlendShapeData { get; set; }

    [JsonIgnore]
    public Memory<Matrix4X4>? BindPose { get; set; }

    public uint[] BoneNameHashes { get; set; } = Array.Empty<uint>();
    public uint RootBoneNameHash { get; set; }
    public List<AABB> BonesAABB { get; set; } = new();

    [JsonIgnore]
    public Memory<uint>? VariableBoneCountWeights { get; set; }

    public MeshCompression MeshCompression { get; set; }
    public bool IsReadable { get; set; }
    public bool KeepVertices { get; set; }
    public bool KeepIndices { get; set; }
    public IndexFormat IndexFormat { get; set; }

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

    private bool ShouldDeserializeBindPose => BindPoseStart > -1 && BindPose == null;
    private bool ShouldDeserializeVariableBoneCountWeights => VariableBoneCountWeightsStart > -1 && VariableBoneCountWeights == null;
    private bool ShouldDeserializeIndices => IndicesStart > -1 && Indices == null;
    private bool ShouldDeserializeSkin => SkinStart > -1 && Skin == null;
    private bool ShouldDeserializeBakedConvexCollisionMesh => BakedConvexCollisionMeshStart > -1 && BakedConvexCollisionMesh == null;
    private bool ShouldDeserializeBakedTriangleCollisionMesh => BakedTriangleCollisionMeshStart > -1 && BakedTriangleCollisionMesh == null;

    [JsonIgnore]
    public override bool ShouldDeserialize => base.ShouldDeserialize || ShouldDeserializeBindPose || ShouldDeserializeVariableBoneCountWeights || ShouldDeserializeIndices || ShouldDeserializeSkin || ShouldDeserializeBakedConvexCollisionMesh || ShouldDeserializeBakedTriangleCollisionMesh || VertexData.ShouldDeserialize || CompressedMesh.ShouldDeserialize || BlendShapeData.ShouldDeserialize;

    public long StreamDataOffset { get; set; }
    public StreamingInfo StreamData { get; set; }

    public override void Free() {
        if (IsMutated) {
            return;
        }

        base.Free();
        BlendShapeData.Free();
        BindPose = null;
        VariableBoneCountWeights = null;
        Indices = null;
        Skin = null;
        VertexData.Free();
        CompressedMesh.Free();
        BakedConvexCollisionMesh = null;
        BakedTriangleCollisionMesh = null;
    }

    public override void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        base.Deserialize(reader, options);

        if (BlendShapeData.ShouldDeserialize) {
            BlendShapeData.Deserialize(reader, SerializedFile, options);
        }

        if (ShouldDeserializeBindPose) {
            reader.BaseStream.Seek(BindPoseStart, SeekOrigin.Begin);
            var bindPoseCount = reader.ReadInt32();
            BindPose = reader.ReadMemory<Matrix4X4>(bindPoseCount);
        }

        if (ShouldDeserializeVariableBoneCountWeights) {
            reader.BaseStream.Seek(VariableBoneCountWeightsStart, SeekOrigin.Begin);
            var variableBoneCountWeightsCount = reader.ReadInt32();
            VariableBoneCountWeights = reader.ReadMemory<uint>(variableBoneCountWeightsCount);
        }

        if (ShouldDeserializeIndices) {
            reader.BaseStream.Seek(IndicesStart, SeekOrigin.Begin);
            var indicesCount = reader.ReadInt32();
            Indices = reader.ReadMemory(indicesCount);
        }

        if (ShouldDeserializeSkin) {
            reader.BaseStream.Seek(SkinStart, SeekOrigin.Begin);
            var boneWeightsCount = reader.ReadInt32();
            Skin = new List<BoneWeight>();
            Skin.EnsureCapacity(boneWeightsCount);
            for (var i = 0; i < boneWeightsCount; ++i) {
                Skin.Add(BoneWeight.FromReader(reader, SerializedFile));
            }
        }

        if (VertexData.ShouldDeserialize) {
            VertexData.Deserialize(reader, SerializedFile, options);

            if (!StreamData.IsNull) {
                VertexData.Data = StreamData.GetData(SerializedFile.Assets, options, VertexData.Data);
            }
        }

        if (CompressedMesh.ShouldDeserialize) {
            CompressedMesh.Deserialize(reader, SerializedFile, options);
        }

        if (ShouldDeserializeBakedConvexCollisionMesh) {
            reader.BaseStream.Seek(BakedConvexCollisionMeshStart, SeekOrigin.Begin);
            var bakedMeshCollisionMeshSize = reader.ReadInt32();
            BakedConvexCollisionMesh = reader.ReadMemory(bakedMeshCollisionMeshSize);
        }

        if (ShouldDeserializeBakedTriangleCollisionMesh) {
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
