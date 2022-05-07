using System;
using System.Collections.Concurrent;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Snuggle.Core.Implementations;
using Snuggle.Core.Options;

namespace Snuggle.Converters;

public static class SnuggleTextureFile {
    static SnuggleTextureFile() {
        var old = Configuration.Default.MemoryAllocator;
        Configuration.Default.MemoryAllocator = new SimpleGcMemoryAllocator();
        old.ReleaseRetainedResources();
    }

    private static ConcurrentDictionary<(long, string), ReadOnlyMemory<byte>> CachedData { get; set; } = new();

    public static Memory<byte> LoadCachedTexture(Texture2D texture, bool useDirectXTex, bool useTextureDecoder) {
        var memory = CachedData.GetOrAdd(
            texture.GetCompositeId(),
            static (_, arg) => {
                var (texture, useDirectXTex, useTextureDecoder) = arg;
                texture.Deserialize(ObjectDeserializationOptions.Default);
                var data = Texture2DConverter.ToRGBA(texture, useDirectXTex && Environment.OSVersion.Platform == PlatformID.Win32NT, useTextureDecoder);
                return data;
            },
            (texture, useDirectXTex, useTextureDecoder));
        var newMemory = new Memory<byte>(new byte[memory.Length]);
        memory.CopyTo(newMemory);
        return newMemory;
    }

    public static void ClearMemory() {
        CachedData.Clear();
        CachedData = new ConcurrentDictionary<(long, string), ReadOnlyMemory<byte>>();
        Configuration.Default.MemoryAllocator.ReleaseRetainedResources();
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
        SavePNG(texture, path, flip, options.UseDirectTex, options.UseTextureDecoder);
        return path;
    }

    public static void SavePNG(Texture2D texture, string path, bool flip, bool useDirectXTex, bool useTextureDecoder) {
        if (File.Exists(path)) {
            return;
        }

        using var image = ConvertImage(texture, flip, useDirectXTex, useTextureDecoder);
        image.SaveAsPng(path);
    }

    public static Image<Rgba32> ConvertImage(Texture2D texture, bool flip, bool useDirectXTex, bool useTextureDecoder) {
        var data = LoadCachedTexture(texture, useDirectXTex, useTextureDecoder);
        if (data.IsEmpty) {
            return new Image<Rgba32>(1, 1, new Rgba32(0));
        }

        Image<Rgba32> image;
        if (texture.TextureFormat.IsAlphaFirst()) {
            using var temp = Image.WrapMemory<Argb32>(data, texture.Width, texture.Height);
            image = temp.CloneAs<Rgba32>();
        } else if (texture.TextureFormat.IsBGRA(useDirectXTex, useTextureDecoder)) {
            using var temp = Image.WrapMemory<Bgra32>(data, texture.Width, texture.Height);
            image = temp.CloneAs<Rgba32>();
        } else {
            image = Image.WrapMemory<Rgba32>(data, texture.Width, texture.Height);
        }

        if (flip) {
            image.Mutate(context => context.Flip(FlipMode.Vertical));
        }

        return image;
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
