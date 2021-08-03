using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Objects;
using Equilibrium.Models.Objects.Graphics;
using Equilibrium.Models.Objects.Math;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Equilibrium.Implementations {
    [PublicAPI, ObjectImplementation(UnityClassId.Mesh)]
    public class Mesh : NamedObject {
        public Mesh(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) { }

        public Mesh(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
            Submeshes = new List<Submesh>();
            BlendShapeDatas = new List<BlendShapeData>();
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
        private long BakedConvexCollisionMeshStart { get; set; }
        private long BakedTriangleCollisionMeshStart { get; set; }

        public List<Submesh> Submeshes { get; set; }
        public List<BlendShapeData> BlendShapeDatas { get; set; }

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
        public StreamingInfo StreamData { get; set; }

        public override bool ShouldDeserialize =>
            base.ShouldDeserialize ||
            BindPose == null ||
            VariableBoneCountWeights == null ||
            Indices == null ||
            BakedConvexCollisionMesh == null ||
            BakedTriangleCollisionMesh == null ||
            VertexData.ShouldDeserialize ||
            CompressedMesh.ShouldDeserialize ||
            BlendShapeDatas.Any(x => x.ShouldDeserialize);
    }
}
