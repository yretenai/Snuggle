using System.IO;
using DragonLib;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertGameObject(SnuggleFlags flags, ILogger logger, GameObject gameObject) {
        var path = PathFormatter.Format(flags.OutputFormat, "gltf", gameObject);
        if (File.Exists(path)) {
            return;
        }

        var fullPath = Path.Combine(flags.OutputPath, path);
        fullPath.EnsureDirectoryExists();
        SnuggleMeshFile.Save(gameObject, fullPath, new SnuggleMeshFile.SnuggleMeshFileOptions(ObjectDeserializationOptions.Default, !flags.NoGameObjectHierarchyDown, !flags.NoGameObjectHierarchyUp, flags.TextureToDDS, !flags.NoMaterials, !flags.NoTexture));
        logger.Info($"Saved {path}");
    }
}
