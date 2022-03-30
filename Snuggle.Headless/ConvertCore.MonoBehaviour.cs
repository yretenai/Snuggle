using System.IO;
using System.Text.Json;
using DragonLib;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertMonoBehaviour(SnuggleFlags flags, ILogger logger, MonoBehaviour monoBehaviour) {
        var ext = "json";
        if (Path.HasExtension(monoBehaviour.ObjectContainerPath)) {
            ext = Path.GetExtension(monoBehaviour.ObjectContainerPath)[1..];
        }

        var path = PathFormatter.Format(monoBehaviour.HasContainerPath ? flags.OutputFormat : flags.ContainerlessOutputFormat ?? flags.OutputFormat, ext, monoBehaviour);
        var fullPath = Path.Combine(flags.OutputPath, path);
        if (File.Exists(fullPath)) {
            return;
        }

        fullPath.EnsureDirectoryExists();

        monoBehaviour.Deserialize(ObjectDeserializationOptions.Default);

        File.WriteAllBytes(fullPath, JsonSerializer.SerializeToUtf8Bytes(monoBehaviour, SnuggleCoreOptions.JsonOptions));
    }
}
