using System;
using System.Collections.Concurrent;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Snuggle.Converters;
using Snuggle.Core.Implementations;

namespace Snuggle.Handlers; 

public static class SnuggleTextureFile {
    private static ConcurrentDictionary<long, Memory<byte>> CachedData { get; } = new();

    public static byte[] LoadCachedTexture(Texture2D texture) {
        return CachedData.GetOrAdd(texture.PathId,
                (_, arg) => {
                    var data = Texture2DConverter.ToRGBA(arg);
                    if (texture.TextureFormat.IsAlphaFirst()) {
                        for (var i = 0; i < data.Length; i += 4) {
                            var a = data.Span[i];
                            var r = data.Span[i + 1];
                            var g = data.Span[i + 2];
                            var b = data.Span[i + 3];
                            data.Span[i] = r;
                            data.Span[i + 1] = g;
                            data.Span[i + 2] = b;
                            data.Span[i + 3] = a;
                        }
                    }

                    if (texture.TextureFormat.IsBGRA()) {
                        for (var i = 0; i < data.Length; i += 4) {
                            var b = data.Span[i];
                            var g = data.Span[i + 1];
                            var r = data.Span[i + 2];
                            data.Span[i] = r;
                            data.Span[i + 1] = g;
                            data.Span[i + 2] = b;
                        }
                    }

                    return data;
                },
                texture)
            .ToArray();
    }

    public static void ClearMemory() {
        CachedData.Clear();
    }

    public static string Save(Texture2D texture, string path) {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) &&
            !Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        if (!SnuggleCore.Instance.Settings.WriteNativeTextures ||
            !SaveNative(texture, path, out var resultPath)) {
            return SavePNG(texture, path);
        }

        return resultPath;
    }

    private static string SavePNG(Texture2D texture, string path) {
        path = Path.ChangeExtension(path, ".png");
        if (File.Exists(path)) {
            return path;
        }

        var data = new Memory<byte>(LoadCachedTexture(texture));
        if (data.IsEmpty) {
            return path;
        }

        var image = Image.WrapMemory<Rgba32>(data, texture.Width, texture.Height);
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