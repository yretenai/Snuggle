using System.IO;
using DragonLib;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertMaterial(SnuggleFlags flags, ILogger logger, Material material) {
        var path = PathFormatter.Format(flags.OutputFormat, "json", material);
        if (File.Exists(path)) {
            return;
        }

        var fullPath = Path.Combine(flags.OutputPath, path);
        fullPath.EnsureDirectoryExists();
        SnuggleMaterialFile.SaveMaterial(material, fullPath, false);
        logger.Info($"Saved {path}");
    }
}
