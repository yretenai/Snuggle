using System.IO;
using DragonLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertSprite(SnuggleFlags flags, Sprite sprite) {
        var path = PathFormatter.Format(sprite.HasContainerPath ? flags.OutputFormat : flags.ContainerlessOutputFormat ?? flags.OutputFormat, "png", sprite);
        var fullPath = Path.Combine(flags.OutputPath, path);
        if (!flags.Overwrite && File.Exists(fullPath)) {
            return;
        }

        fullPath.EnsureDirectoryExists();

        sprite.Deserialize(ObjectDeserializationOptions.Default);

        var (data, (width, height), _) = SnuggleSpriteFile.ConvertSprite(sprite, ObjectDeserializationOptions.Default, flags.UseTextureDecoder);
        var image = Image.WrapMemory<Rgba32>(data, width, height);

        image.SaveAsPng(fullPath);
    }
}
