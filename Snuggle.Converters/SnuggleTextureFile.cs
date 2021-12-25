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
    private static ConcurrentDictionary<(long, string), ReadOnlyMemory<byte>> CachedData { get; } = new();

    public static Memory<byte> LoadCachedTexture(Texture2D texture, bool useDirectXTex) {
        var memory = CachedData.GetOrAdd(
            texture.GetCompositeId(),
            static (_, arg) => {
                var (texture, useDirectXTex) = arg;
                texture.Deserialize(ObjectDeserializationOptions.Default);
                var data = Texture2DConverter.ToRGBA(texture, useDirectXTex && Environment.OSVersion.Platform == PlatformID.Win32NT);
                return data;
            },
            (texture, useDirectXTex));
        var newMemory = new Memory<byte>(new byte[memory.Length]);
        memory.CopyTo(newMemory);
        return newMemory;
    }

    public static void ClearMemory() {
        CachedData.Clear();
    }

    public static string Save(Texture2D texture, string path, SnuggleExportOptions options, bool flip) {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        var writeDds = options.WriteNativeTextures && texture.TextureFormat.CanSupportDDS();

        if (writeDds) {
            path = Path.ChangeExtension(path, ".dds");
            SaveNative(texture, path);
            return path;
        }

        path = Path.ChangeExtension(path, ".png");
        SavePNG(texture, path, flip, options.UseDirectTex);
        return path;
    }

    public static void SavePNG(Texture2D texture, string path, bool flip, bool useDirectXTex) {
        if (File.Exists(path)) {
            return;
        }

        using var image = ConvertImage(texture, flip, useDirectXTex);
        image.SaveAsPng(path);
    }

    public static Image<Rgba32> ConvertImage(Texture2D texture, bool flip, bool useDirectXTex) {
        var data = LoadCachedTexture(texture, useDirectXTex);
        if (data.IsEmpty) {
            return new Image<Rgba32>(1, 1, new Rgba32(0));
        }

        Image image;
        if (texture.TextureFormat.IsAlphaFirst()) {
            image = Image.WrapMemory<Argb32>(data, texture.Width, texture.Height);
        } else if (texture.TextureFormat.IsBGRA(useDirectXTex)) {
            image = Image.WrapMemory<Bgra32>(data, texture.Width, texture.Height);
        } else {
            image = Image.WrapMemory<Rgba32>(data, texture.Width, texture.Height);
        }

        if (flip) {
            image.Mutate(context => context.Flip(FlipMode.Vertical));
        }

        var cloned = image.CloneAs<Rgba32>();
        image.Dispose();
        return cloned;
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
