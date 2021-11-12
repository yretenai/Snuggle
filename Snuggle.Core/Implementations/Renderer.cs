using System.Collections.Generic;
using JetBrains.Annotations;
using Snuggle.Core.Extensions;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Models.Serialization;

namespace Snuggle.Core.Implementations; 

[PublicAPI, ObjectImplementation(UnityClassId.Renderer)]
public class Renderer : Behaviour {
    public Renderer(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        if (SerializedFile.Version < UnityVersionRegister.Unity5_4) {
            reader.Align();
        }

        CastShadows = reader.ReadByte();
        RecieveShadows = reader.ReadByte();
        if (SerializedFile.Version >= UnityVersionRegister.Unity2017_2) {
            DynamicOccludee = reader.ReadByte();
        }

        if (SerializedFile.Version >= UnityVersionRegister.Unity5_4) {
            MotionVectors = reader.ReadByte();
            LightProbeUsage = reader.ReadByte();
            ReflectionProbeUsage = reader.ReadByte();
        }

        if (SerializedFile.Version >= UnityVersionRegister.Unity2019_3) {
            RayTracingMode = reader.ReadByte();
        }

        if (SerializedFile.Version >= UnityVersionRegister.Unity2020_1) {
            RayTraceProcedural = reader.ReadByte();
        }

        reader.Align();

        if (SerializedFile.Version >= UnityVersionRegister.Unity2018_1) {
            RenderingLayerMask = reader.ReadUInt32();
        }

        if (SerializedFile.Version >= UnityVersionRegister.Unity2018_3) {
            RenderingPriority = reader.ReadInt32();
        }

        LightmapIndex = reader.ReadUInt16();
        DynamicLightmapIndex = reader.ReadUInt16();
        LightmapTilingOffset = reader.ReadStruct<Vector4>();
        DynamicLightmapTilingOffset = reader.ReadStruct<Vector4>();

        var materialCount = reader.ReadInt32();
        Materials.AddRange(PPtr<Material>.ArrayFromReader(reader, SerializedFile, materialCount));

        if (SerializedFile.Version < UnityVersionRegister.Unity5_5) {
            var subsetIndiceCount = reader.ReadInt32();
            SubsetIndices.AddRange(reader.ReadArray<int>(subsetIndiceCount));
            StaticBatchInfo = StaticBatchInfo.FromSubsetIndices(SubsetIndices);
        } else {
            StaticBatchInfo = StaticBatchInfo.FromReader(reader, SerializedFile);
        }

        StaticBatchRoot = PPtr<Transform>.FromReader(reader, SerializedFile);

        if (SerializedFile.Version < UnityVersionRegister.Unity5_4) {
            UseLightProbes = reader.ReadBoolean();
            ReflectionProbeUsage = reader.ReadInt32();
        }

        ProbeAnchor = PPtr<Transform>.FromReader(reader, SerializedFile);

        if (SerializedFile.Version >= UnityVersionRegister.Unity5_4) {
            LightProbeVolumeOverride = PPtr<GameObject>.FromReader(reader, SerializedFile);
        } else {
            LightProbeVolumeOverride = PPtr<GameObject>.Null;
        }

        SortingLayerId = reader.ReadInt32();
        if (SerializedFile.Version >= UnityVersionRegister.Unity5_4) {
            SortingLayer = reader.ReadInt16();
        }

        SortingOrder = reader.ReadInt16();
    }

    public Renderer(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
        StaticBatchRoot = PPtr<Transform>.Null;
        StaticBatchInfo = StaticBatchInfo.Default;
        ProbeAnchor = PPtr<Transform>.Null;
        LightProbeVolumeOverride = PPtr<GameObject>.Null;
    }

    public byte CastShadows { get; set; }
    public byte RecieveShadows { get; set; }
    public byte DynamicOccludee { get; set; }
    public byte MotionVectors { get; set; }
    public byte LightProbeUsage { get; set; }
    public byte RayTracingMode { get; set; }
    public byte RayTraceProcedural { get; set; }
    public uint RenderingLayerMask { get; set; }
    public int RenderingPriority { get; set; }
    public ushort LightmapIndex { get; set; }
    public ushort DynamicLightmapIndex { get; set; }
    public Vector4 LightmapTilingOffset { get; set; }
    public Vector4 DynamicLightmapTilingOffset { get; set; }
    public List<PPtr<Material>> Materials { get; set; } = new();
    public List<int> SubsetIndices { get; set; } = new();
    public StaticBatchInfo StaticBatchInfo { get; set; }
    public PPtr<Transform> StaticBatchRoot { get; set; }
    public bool UseLightProbes { get; set; }
    public int ReflectionProbeUsage { get; set; }
    public PPtr<Transform> ProbeAnchor { get; set; }
    public PPtr<GameObject> LightProbeVolumeOverride { get; set; }
    public int SortingLayerId { get; set; }
    public short SortingLayer { get; set; }
    public short SortingOrder { get; set; }
}