using System.IO;
using System.Linq;
using System.Text.Json;
using DragonLib;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertCABPathProvider(SnuggleFlags flags, ICABPathProvider cabPathProvider) {
        var ext = "json";
        var objectVer = (SerializedObject) cabPathProvider;
        if (Path.HasExtension(objectVer.ObjectContainerPath)) {
            ext = Path.GetExtension(objectVer.ObjectContainerPath)[1..];
        }

        var path = PathFormatter.Format(objectVer.HasContainerPath ? flags.OutputFormat : flags.ContainerlessOutputFormat ?? flags.OutputFormat, ext, objectVer);
        var fullPath = Path.Combine(flags.OutputPath, path);
        if (!flags.Overwrite && File.Exists(fullPath)) {
            return;
        }

        fullPath.EnsureDirectoryExists();

        objectVer.Deserialize(ObjectDeserializationOptions.Default);

        File.WriteAllBytes(fullPath, JsonSerializer.SerializeToUtf8Bytes(cabPathProvider.GetCABPaths().Select(x => new { Ptr = x.Key, Path = x.Value}), SnuggleCoreOptions.JsonOptions));
    }
}
