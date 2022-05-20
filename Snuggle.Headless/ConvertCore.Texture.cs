using System.IO;
using DragonLib;
using Snuggle.Converters;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertTexture(SnuggleFlags flags, ITexture texture, bool flip) {
        var dds = flags.WriteNativeTextures && texture.TextureFormat.CanSupportDDS();

        var path = PathFormatter.Format(texture.HasContainerPath ? flags.OutputFormat : flags.ContainerlessOutputFormat ?? flags.OutputFormat, dds ? "dds" : "png", texture);
        var fullPath = Path.Combine(flags.OutputPath, path);
        if (!flags.Overwrite && File.Exists(fullPath)) {
            return;
        }

        fullPath.EnsureDirectoryExists();

        texture.Deserialize(ObjectDeserializationOptions.Default);

        if (dds) {
            SnuggleTextureFile.SaveNative(texture, fullPath);
        } else {
            SnuggleTextureFile.SavePNG(texture, fullPath, flip, flags.UseTextureDecoder);
        }
    }
}
