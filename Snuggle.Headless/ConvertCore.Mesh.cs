using System.IO;
using DragonLib;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertMesh(SnuggleFlags flags, ILogger logger, Mesh mesh) {
        var path = PathFormatter.Format(flags.OutputFormat, "gltf", mesh);
        if (File.Exists(path)) {
            return;
        }
        
        mesh.Deserialize(ObjectDeserializationOptions.Default);

        var fullPath = Path.Combine(flags.OutputPath, path);
        fullPath.EnsureDirectoryExists();
        SnuggleMeshFile.Save(mesh, fullPath, new SnuggleMeshFile.SnuggleMeshFileOptions(ObjectDeserializationOptions.Default, !flags.NoGameObjectHierarchyDown, !flags.NoGameObjectHierarchyUp, flags.TextureToDDS, !flags.NoMaterials, !flags.NoTexture, !flags.NoVertexColor));
        logger.Info($"Saved {path}");
    }
}
