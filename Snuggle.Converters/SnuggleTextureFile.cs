using System;
using System.Collections.Concurrent;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Snuggle.Core.Implementations;
using Snuggle.Core.Options;

namespace Snuggle.Converters;

public static class SnuggleTextureFile {
    private static ConcurrentDictionary<long, Memory<byte>> CachedData { get; } = new();

    public static byte[] LoadCachedTexture(Texture2D texture) {
        return CachedData.GetOrAdd(
                texture.PathId,
                static (_, arg) => {
                    arg.Deserialize(ObjectDeserializationOptions.Default);
                    var data = Texture2DConverter.ToRGBA(arg);
                    return data;
                },
                texture)
            .ToArray();
    }

    public static void ClearMemory() {
        CachedData.Clear();
    }

    public static string Save(Texture2D texture, string path, bool writeNative, bool flip) {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        if (!writeNative || !SaveNative(texture, path, out var resultPath)) {
            return SavePNG(texture, path, flip);
        }

        return resultPath;
    }

    private static string SavePNG(Texture2D texture, string path, bool flip) {
        path = Path.ChangeExtension(path, ".png");
        if (File.Exists(path)) {
            return path;
        }

        var data = new Memory<byte>(LoadCachedTexture(texture));
        if (data.IsEmpty) {
            return path;
        }

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

        image.SaveAsPng(path);
        
        return path;
    }

    private static bool SaveNative(Texture2D texture, string path, out string destinationPath) {
        destinationPath = Path.ChangeExtension(path, ".dds");
        if (!texture.TextureFormat.CanSupportDDS()) {
            return false;
        }

        if (File.Exists(destinationPath)) {
            return true;
        }

        using var fs = File.OpenWrite(destinationPath);
        fs.SetLength(0);
        fs.Write(Texture2DConverter.ToDDS(texture));
        return true;
    }
}
