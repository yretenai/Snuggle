using System.IO;
using DragonLib;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertGameObject(SnuggleFlags flags, GameObject gameObject) {
        var path = PathFormatter.Format(gameObject.HasContainerPath ? flags.OutputFormat : flags.ContainerlessOutputFormat ?? flags.OutputFormat, "gltf", gameObject);
        var fullPath = Path.Combine(flags.OutputPath, path);
        if (!flags.Overwrite && File.Exists(fullPath)) {
            return;
        }

        fullPath.EnsureDirectoryExists();

        SnuggleMeshFile.Save(
            gameObject,
            fullPath,
            ObjectDeserializationOptions.Default,
            SnuggleExportOptions.Default with { WriteNativeTextures = flags.WriteNativeTextures, OnlyWithCABPath = flags.OnlyCAB },
            SnuggleMeshExportOptions.Default with {
                FindGameObjectDescendants = !flags.NoGameObjectHierarchyDown,
                FindGameObjectParents = !flags.NoGameObjectHierarchyUp,
                WriteMaterial = !flags.NoMaterials,
                WriteTexture = !flags.NoTexture,
                WriteVertexColors = !flags.NoVertexColor,
                WriteMorphs = !flags.NoMorphs,
                MirrorXPosition = !flags.KeepXPos,
                MirrorXNormal = !flags.KeepXNorm,
                MirrorXTangent = !flags.KeepXTan,
            });
    }
}
