using System.IO;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertTexture(SnuggleFlags flags, ILogger logger, Texture2D texture, bool flip) {
        var dds = flags.TextureToDDS && texture.TextureFormat.CanSupportDDS();
        var path = PathFormatter.Format(flags.OutputFormat, dds ? "dds" : "png", texture);
        if (File.Exists(path)) {
            return;
        }

        if (dds) {
            SnuggleTextureFile.SaveNative(texture, path);
        } else {
            SnuggleTextureFile.SavePNG(texture, path, flip);
        }

        logger.Info($"Saved {path}");
    }
}
