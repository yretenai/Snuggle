using System.IO;
using DragonLib;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertMesh(SnuggleFlags flags, ILogger logger, Mesh mesh) {
        mesh.Deserialize(ObjectDeserializationOptions.Default);

        var path = PathFormatter.Format(flags.OutputFormat, "gltf", mesh);
        var fullPath = Path.Combine(flags.OutputPath, path);
        if (File.Exists(fullPath)) {
            return;
        }

        fullPath.EnsureDirectoryExists();

        SnuggleMeshFile.Save(
            mesh,
            fullPath,
            ObjectDeserializationOptions.Default,
            SnuggleExportOptions.Default with { WriteNativeTextures = flags.TextureToDDS, UseDirectTex = flags.UseDirectXTex, OnlyWithCABPath = flags.OnlyCAB },
            SnuggleMeshExportOptions.Default with {
                FindGameObjectDescendants = !flags.NoGameObjectHierarchyDown,
                FindGameObjectParents = !flags.NoGameObjectHierarchyUp,
                WriteMaterial = !flags.NoMaterials,
                WriteTexture = !flags.NoTexture,
                WriteVertexColors = !flags.NoVertexColor,
                WriteMorphs = !flags.NoMorphs,
            });
        logger.Info($"Saved {path}");
    }
}
