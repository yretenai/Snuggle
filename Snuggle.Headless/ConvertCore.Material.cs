using System.IO;
using DragonLib;
using Snuggle.Converters;
using Snuggle.Core.Implementations;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertMaterial(SnuggleFlags flags, Material material) {
        var path = PathFormatter.Format(material.HasContainerPath ? flags.OutputFormat : flags.ContainerlessOutputFormat ?? flags.OutputFormat, "json", material);
        var fullPath = Path.Combine(flags.OutputPath, path);
        if (!flags.Overwrite && File.Exists(fullPath)) {
            return;
        }

        fullPath.EnsureDirectoryExists();

        SnuggleMaterialFile.Save(material, fullPath, false);
    }
}
