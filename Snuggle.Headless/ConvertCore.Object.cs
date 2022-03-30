using System.IO;
using System.Text.Json;
using DragonLib;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertObject(SnuggleFlags flags, ILogger logger, SerializedObject serializedObject) {
        var ext = "json";
        if (Path.HasExtension(serializedObject.ObjectContainerPath)) {
            ext = Path.GetExtension(serializedObject.ObjectContainerPath)[1..];
        }

        var path = PathFormatter.Format(serializedObject.HasContainerPath ? flags.OutputFormat : flags.ContainerlessOutputFormat ?? flags.OutputFormat, ext, serializedObject);
        var fullPath = Path.Combine(flags.OutputPath, path);
        if (File.Exists(fullPath)) {
            return;
        }

        fullPath.EnsureDirectoryExists();

        serializedObject.Deserialize(ObjectDeserializationOptions.Default);

        File.WriteAllBytes(fullPath, JsonSerializer.SerializeToUtf8Bytes(serializedObject, SnuggleCoreOptions.JsonOptions));
    }
}
