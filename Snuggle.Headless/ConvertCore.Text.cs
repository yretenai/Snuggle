using System.IO;
using DragonLib;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertText(SnuggleFlags flags, ILogger logger, Text text) {
        var ext = "txt";
        if (Path.HasExtension(text.ObjectContainerPath)) {
            ext = Path.GetExtension(text.ObjectContainerPath)[1..];
        }
        
        var path = PathFormatter.Format(flags.OutputFormat, ext, text);
        if (File.Exists(path)) {
            return;
        }

        text.Deserialize(ObjectDeserializationOptions.Default);
        var fullPath = Path.Combine(flags.OutputPath, path);
        fullPath.EnsureDirectoryExists();
        File.WriteAllBytes(fullPath, text.String!.Value.ToArray());
        logger.Info($"Saved {path}");
    }
}
