using System.IO;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertText(SnuggleFlags flags, Text text) {
        var path = PathFormatter.Format(flags.OutputFormat, "txt", text);
        if (File.Exists(path)) {
            return;
        }

        text.Deserialize(ObjectDeserializationOptions.Default);

        File.WriteAllText(path, text.String);
    }
}
