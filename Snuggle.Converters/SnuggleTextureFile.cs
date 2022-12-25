using System.Collections.Concurrent;
using System.IO;
using CommunityToolkit.HighPerformance.Buffers;
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

    private static ConcurrentDictionary<(long, string), MemoryOwner<byte>> CachedData { get; set; } = new();

    public static MemoryOwner<byte> LoadCachedTexture(ITexture texture) {
        return CachedData.GetOrAdd(
            texture.GetCompositeId(),
            static (_, texture) => {
                texture.Deserialize(ObjectDeserializationOptions.Default);
                return Texture2DConverter.ToPixels(texture);
            },
            texture);
    }

    public static void ClearMemory() {
        foreach(var (_, data) in CachedData) {
            data.Dispose();
        }
        
        CachedData.Clear();
        CachedData = new ConcurrentDictionary<(long, string), MemoryOwner<byte>>();
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

    public static Image<Bgra32> ConvertImage(ITexture texture, bool flip) {
        var data = LoadCachedTexture(texture);
        if (data.Length == 0) {
            return new Image<Bgra32>(1, 1, new Bgra32(0, 0, 0, 0xFF));
        }

        var image = Image.WrapMemory<Bgra32>(data, texture.Width, texture.Height);

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
