using System;
using System.Collections.Generic;
using Snuggle.Core.Extensions;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[ObjectImplementation(UnityClassId.SkinnedMeshRenderer)]
public class SkinnedMeshRenderer : Renderer {
    public SkinnedMeshRenderer(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        Quality = reader.ReadInt32();

        UpdateWhenOffscreen = reader.ReadBoolean();
        if (SerializedFile.Version >= UnityVersionRegister.Unity5_4) {
            SkinnedMotionVectors = reader.ReadBoolean();
        }

        reader.Align();

        Mesh = PPtr<Mesh>.FromReader(reader, SerializedFile);

        var boneCount = reader.ReadInt32();
        Bones.AddRange(PPtr<Transform>.ArrayFromReader(reader, SerializedFile, boneCount));

        var blendShapeWeightCount = reader.ReadInt32();
        BlendShapeWeights.EnsureCapacity(blendShapeWeightCount);
        BlendShapeWeights.AddRange(reader.ReadSpan<float>(blendShapeWeightCount));

        RootBone = PPtr<Transform>.FromReader(reader, SerializedFile);
        AABB = reader.ReadStruct<AABB>();

        DirtyAABB = reader.ReadBoolean();
        reader.Align();
    }

    public SkinnedMeshRenderer(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
        Mesh = PPtr<Mesh>.Null;
        RootBone = PPtr<Transform>.Null;
    }

    public int Quality { get; set; }
    public bool UpdateWhenOffscreen { get; set; }
    public bool SkinnedMotionVectors { get; set; }
    public PPtr<Mesh> Mesh { get; set; }
    public List<PPtr<Transform>> Bones { get; set; } = new();
    public List<float> BlendShapeWeights { get; set; } = new();
    public PPtr<Transform> RootBone { get; set; }
    public AABB AABB { get; set; }
    public bool DirtyAABB { get; set; }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        throw new NotImplementedException();
    }
}
