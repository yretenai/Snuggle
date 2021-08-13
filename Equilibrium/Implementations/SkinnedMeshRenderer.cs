using System.Collections.Generic;
using Equilibrium.Extensions;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Objects.Math;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, ObjectImplementation(UnityClassId.SkinnedMeshRenderer)]
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
            BlendShapeWeights.AddRange(reader.ReadArray<float>(blendShapeWeightCount));

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
    }
}
