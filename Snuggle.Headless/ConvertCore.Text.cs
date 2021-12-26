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
        var fullPath = Path.Combine(flags.OutputPath, path);
        if (File.Exists(fullPath)) {
            return;
        }

        fullPath.EnsureDirectoryExists();

        text.Deserialize(ObjectDeserializationOptions.Default);

        File.WriteAllBytes(fullPath, text.String!.Value.ToArray());
        logger.Info($"Saved {path}");
    }
}
