using System;
using System.Collections.Concurrent;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Converters;

public static class SnuggleTextureFile {
    static SnuggleTextureFile() {
        var old = Configuration.Default.MemoryAllocator;
        Configuration.Default.MemoryAllocator = new SimpleGcMemoryAllocator();
        old.ReleaseRetainedResources();
    }

    private static ConcurrentDictionary<(long, string), ReadOnlyMemory<byte>> CachedData { get; set; } = new();

    public static Memory<byte> LoadCachedTexture(ITexture texture) {
        var memory = CachedData.GetOrAdd(
            texture.GetCompositeId(),
            static (_, texture) => {
                texture.Deserialize(ObjectDeserializationOptions.Default);
                var data = Texture2DConverter.ToRGBA(texture);
                return data;
            },
            texture);
        var newMemory = new Memory<byte>(new byte[memory.Length]);
        memory.CopyTo(newMemory);
        return newMemory;
    }

    public static void ClearMemory() {
        CachedData.Clear();
        CachedData = new ConcurrentDictionary<(long, string), ReadOnlyMemory<byte>>();
        Configuration.Default.MemoryAllocator.ReleaseRetainedResources();
    }

    public static string Save(ITexture texture, string path, SnuggleExportOptions options, bool flip) {
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
        SavePNG(texture, path, flip);
        return path;
    }

    public static void SavePNG(ITexture texture, string path, bool flip) {
        if (File.Exists(path)) {
            return;
        }

        using var image = ConvertImage(texture, flip);
        image.SaveAsPng(path);
    }

    public static Image<Rgba32> ConvertImage(ITexture texture, bool flip) {
        var data = LoadCachedTexture(texture);
        if (data.IsEmpty) {
            return new Image<Rgba32>(1, 1, new Rgba32(0));
        }

        var image = Image.WrapMemory<Rgba32>(data, texture.Width, texture.Height);

        if (flip) {
            image.Mutate(context => context.Flip(FlipMode.Vertical));
        }

        return image;
    }

    public static void SaveNative(ITexture texture, string path) {
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
