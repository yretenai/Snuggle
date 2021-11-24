using System;
using System.Collections.Concurrent;
using System.IO;
using DragonLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    private static ConcurrentDictionary<long, Memory<byte>> CachedData { get; } = new();

    public static void ConvertTexture(SnuggleFlags flags, ILogger logger, Texture2D texture, bool flip) {
        var path = PathFormatter.Format(flags.OutputFormat, "png", texture);
        if (File.Exists(path)) {
            return;
        }

        var data = new Memory<byte>(LoadCachedTexture(texture));
        if (data.IsEmpty) {
            return;
        }
        
        var fullPath = Path.Combine(flags.OutputPath, path);
        fullPath.EnsureDirectoryExists();

        Image image;
        if (texture.TextureFormat.IsAlphaFirst()) {
            image = Image.WrapMemory<Argb32>(data, texture.Width, texture.Height);
        } else if (texture.TextureFormat.IsBGRA() || !texture.TextureFormat.CanSupportDDS()) {
            image = Image.WrapMemory<Bgra32>(data, texture.Width, texture.Height);
        } else {
            image = Image.WrapMemory<Rgba32>(data, texture.Width, texture.Height);
        }

        if (flip) {
            image.Mutate(context => context.Flip(FlipMode.Vertical));
        }

        image.SaveAsPng(fullPath);
        
        logger.Info($"Saved {path}");
    }

    private static byte[] LoadCachedTexture(Texture2D texture) {
        return CachedData.GetOrAdd(
                texture.PathId,
                static (_, arg) => {
                    arg.Deserialize(ObjectDeserializationOptions.Default);
                    return Texture2DConverter.ToRGBA(arg);
                },
                texture)
            .ToArray();
    }

    private static void ClearTexMemory() {
        CachedData.Clear();
    }
}
