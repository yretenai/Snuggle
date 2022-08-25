using System;
using System.IO;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4;
using SevenZip;
using SevenZip.Compression.LZMA;
using Snuggle.Core.Models;

namespace Snuggle.Core;

public static class Utils {
    private static readonly CoderPropID[] PropIDs = { CoderPropID.DictionarySize, CoderPropID.PosStateBits, CoderPropID.LitContextBits, CoderPropID.LitPosBits, CoderPropID.Algorithm, CoderPropID.NumFastBytes, CoderPropID.MatchFinder, CoderPropID.EndMarker };

    private static readonly object[] Properties = { 1 << 23, 2, 3, 0, 2, 128, "bt4", false };

    internal static Stream DecodeLZMA(Stream inStream, int compressedSize, int size, Stream? outStream = null) {
        outStream ??= new MemoryStream(size) { Position = 0 };
        var coder = new Decoder();
        Span<byte> properties = stackalloc byte[5];
        inStream.ReadExactly(properties);
        coder.SetDecoderProperties(properties.ToArray());
        coder.Code(inStream, outStream, compressedSize - 5, size, null);
        return outStream;
    }

    public static void EncodeLZMA(Stream outStream, Stream inStream, long size, CoderPropID[]? propIds = null, object[]? properties = null) {
        var coder = new Encoder();
        coder.SetCoderProperties(propIds ?? PropIDs, properties ?? Properties);
        coder.WriteCoderProperties(outStream);
        coder.Code(inStream, outStream, size, -1, null);
    }

    public static void EncodeLZMA(Stream outStream, Span<byte> inStream, long size, CoderPropID[]? propIds = null, object[]? properties = null) {
        var coder = new Encoder();
        using var ms = new MemoryStream(inStream.ToArray()) { Position = 0 };
        coder.SetCoderProperties(propIds ?? PropIDs, properties ?? Properties);
        coder.WriteCoderProperties(outStream);
        coder.Code(ms, outStream, size, -1, null);
    }

    public static Stream DecompressLZ4(Stream inStream, int compressedSize, int size, Stream? outStream = null) {
        outStream ??= new MemoryStream(size) { Position = 0 };
        var inPool = new byte[compressedSize].AsSpan();
        var outPool = new byte[size].AsSpan();
        inStream.ReadExactly(inPool);
        var amount = LZ4Codec.Decode(inPool, outPool);
        outStream.Write(outPool[..amount]);
        return outStream;
    }

    public static void CompressLZ4(Stream inStream, Stream outStream, LZ4Level level, int size) {
        var inPool = new byte[size].AsSpan();
        var outPool = new byte[LZ4Codec.MaximumOutputSize(size)].AsSpan();
        inStream.ReadExactly(inPool);
        var amount = LZ4Codec.Encode(inPool, outPool, level);
        outStream.Write(outPool[..amount]);
    }

    public static void CompressLZ4(Span<byte> inPool, Stream outStream, LZ4Level level) {
        var outPool = new byte[LZ4Codec.MaximumOutputSize(inPool.Length)].AsSpan();
        var amount = LZ4Codec.Encode(inPool, outPool, level);
        outStream.Write(outPool[..amount]);
    }

    public static float[] UnwrapRGBA(uint rgba) {
        return new[] { (rgba & 0xFF) / (float) 0xFF, ((rgba >> 8) & 0xFF) / (float) 0xFF, ((rgba >> 16) & 0xFF) / (float) 0xFF, ((rgba >> 24) & 0xFF) / (float) 0xFF };
    }

    public static Memory<byte> AsBytes<T>(this Memory<T> memory) where T : struct => new(MemoryMarshal.AsBytes(memory.Span).ToArray());

    public static bool ClassIdIsNamedObject(object classId) {
        return classId is
            UnityClassId.AnimationClip or
            UnityClassId.AnimatorController or
            UnityClassId.AnimatorOverrideController or
            UnityClassId.AnimatorState or
            UnityClassId.AnimatorStateMachine or
            UnityClassId.AnimatorStateTransition or
            UnityClassId.AnimatorTransition or
            UnityClassId.AnimatorTransitionBase or
            UnityClassId.AssemblyDefinitionAsset or
            UnityClassId.AssemblyDefinitionImporter or
            UnityClassId.AssemblyDefinitionReferenceAsset or
            UnityClassId.AssemblyDefinitionReferenceImporter or
            UnityClassId.AssetBundle or
            UnityClassId.AssetBundleManifest or
            UnityClassId.AssetImportInProgressProxy or
            UnityClassId.AssetImporter or
            UnityClassId.AssetImporterLog or
            UnityClassId.AudioClip or
            UnityClassId.AudioImporter or
            UnityClassId.AudioMixer or
            UnityClassId.AudioMixerController or
            UnityClassId.AudioMixerEffectController or
            UnityClassId.AudioMixerGroup or
            UnityClassId.AudioMixerGroupController or
            UnityClassId.AudioMixerSnapshot or
            UnityClassId.AudioMixerSnapshotController or
            UnityClassId.Avatar or
            UnityClassId.AvatarMask or
            UnityClassId.BaseAnimationTrack or
            UnityClassId.BaseVideoTexture or
            UnityClassId.BillboardAsset or
            UnityClassId.BlendTree or
            UnityClassId.BuildReport or
            UnityClassId.CachedSpriteAtlas or
            UnityClassId.CachedSpriteAtlasRuntimeData or
            UnityClassId.ComputeShader or
            UnityClassId.ComputeShaderImporter or
            UnityClassId.Cubemap or
            UnityClassId.CubemapArray or
            UnityClassId.CustomRenderTexture or
            UnityClassId.DefaultAsset or
            UnityClassId.DefaultImporter or
            UnityClassId.EditorProjectAccess or
            UnityClassId.FBXImporter or
            UnityClassId.Flare or
            UnityClassId.Font or
            UnityClassId.GameObjectRecorder or
            UnityClassId.HumanTemplate or
            UnityClassId.IHVImageFormatImporter or
            UnityClassId.LibraryAssetImporter or
            UnityClassId.LightProbes or
            UnityClassId.LightingDataAsset or
            UnityClassId.LightingDataAssetParent or
            UnityClassId.LightingSettings or
            UnityClassId.LightmapParameters or
            UnityClassId.LocalizationAsset or
            UnityClassId.LocalizationImporter or
            UnityClassId.LowerResBlitTexture or
            UnityClassId.Material or
            UnityClassId.Mesh or
            UnityClassId.Mesh3DSImporter or
            UnityClassId.ModelImporter or
            UnityClassId.MonoImporter or
            UnityClassId.MonoScript or
            UnityClassId.Motion or
            UnityClassId.MovieTexture or
            UnityClassId.MultiArtifactTestImporter or
            UnityClassId.NamedObject or
            UnityClassId.NativeFormatImporter or
            UnityClassId.NavMeshData or
            UnityClassId.NewAnimationTrack or
            UnityClassId.OcclusionCullingData or
            UnityClassId.PackageManifest or
            UnityClassId.PackageManifestImporter or
            UnityClassId.PhysicMaterial or
            UnityClassId.PhysicsMaterial2D or
            UnityClassId.PluginImporter or
            UnityClassId.PrefabImporter or
            UnityClassId.PreloadData or
            UnityClassId.Preset or
            UnityClassId.PreviewAnimationClip or
            UnityClassId.ProceduralMaterial or
            UnityClassId.ProceduralTexture or
            UnityClassId.RayTracingShader or
            UnityClassId.RayTracingShaderImporter or
            UnityClassId.ReferencesArtifactGenerator or
            UnityClassId.RenderTexture or
            UnityClassId.RuntimeAnimatorController or
            UnityClassId.SampleClip or
            UnityClassId.SceneAsset or
            UnityClassId.ScriptedImporter or
            UnityClassId.Shader or
            UnityClassId.ShaderImporter or
            UnityClassId.ShaderVariantCollection or
            UnityClassId.SketchUpImporter or
            UnityClassId.SparseTexture or
            UnityClassId.SpeedTreeImporter or
            UnityClassId.SpeedTreeWindAsset or
            UnityClassId.Sprite or
            UnityClassId.SpriteAtlas or
            UnityClassId.SpriteAtlasAsset or
            UnityClassId.SpriteAtlasImporter or
            UnityClassId.SubstanceArchive or
            UnityClassId.SubstanceImporter or
            UnityClassId.TerrainData or
            UnityClassId.TerrainLayer or
            UnityClassId.TextAsset or
            UnityClassId.TextScriptImporter or
            UnityClassId.Texture or
            UnityClassId.Texture2D or
            UnityClassId.Texture2DArray or
            UnityClassId.Texture3D or
            UnityClassId.TextureImporter or
            UnityClassId.TrueTypeFontImporter or
            UnityClassId.VideoClip or
            UnityClassId.VideoClipImporter or
            UnityClassId.VisualEffectAsset or
            UnityClassId.VisualEffectImporter or
            UnityClassId.VisualEffectObject or
            UnityClassId.VisualEffectResource or
            UnityClassId.VisualEffectSubgraph or
            UnityClassId.VisualEffectSubgraphBlock or
            UnityClassId.VisualEffectSubgraphOperator or
            UnityClassId.WebCamTexture;
    }
}
