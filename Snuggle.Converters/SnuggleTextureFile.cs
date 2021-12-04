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

    public static Memory<byte> LoadCachedTexture(Texture2D texture) {
        var memory = CachedData.GetOrAdd(
            texture.PathId,
            static (_, arg) => {
                arg.Deserialize(ObjectDeserializationOptions.Default);
                var data = Texture2DConverter.ToRGBA(arg);
                return data;
            },
            texture);
        var newMemory = new Memory<byte>(new byte[memory.Length]);
        memory.CopyTo(newMemory);
        return newMemory;
    }

    public static void ClearMemory() {
        CachedData.Clear();
    }

    public static string Save(Texture2D texture, string path, bool writeNative, bool flip) {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        var writeDds = writeNative && texture.TextureFormat.CanSupportDDS();

        if (writeDds) {
            path = Path.ChangeExtension(path, ".dds");
            SaveNative(texture, path);
            return path;
        }

        path = Path.ChangeExtension(path, ".png");
        SavePNG(texture, path, flip);
        return path;
    }

    public static void SavePNG(Texture2D texture, string path, bool flip) {
        if (File.Exists(path)) {
            return;
        }

        var data = LoadCachedTexture(texture);
        if (data.IsEmpty) {
            return;
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
    }

    public static void SaveNative(Texture2D texture, string path) {
        if (!texture.TextureFormat.CanSupportDDS()) {
            return;
        }

        if (File.Exists(path)) {
            return;
        }

        using var fs = File.OpenWrite(path);
        fs.SetLength(0);
        fs.Write(Texture2DConverter.ToDDS(texture));
    }
}
