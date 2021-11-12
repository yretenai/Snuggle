using System;
using System.Collections.Concurrent;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    private static ConcurrentDictionary<long, Memory<byte>> CachedData { get; } = new();

    public static void ConvertTexture(SnuggleFlags flags, Texture2D texture) {
        var path = PathFormatter.Format(flags.OutputFormat, "png", texture);
        if (File.Exists(path)) {
            return;
        }

        var data = new Memory<byte>(LoadCachedTexture(texture));
        if (data.IsEmpty) {
            return;
        }

        var image = Image.WrapMemory<Rgba32>(data, texture.Width, texture.Height);
        image.SaveAsPng(path);
    }

    public static byte[] LoadCachedTexture(Texture2D texture) {
        return CachedData.GetOrAdd(
                texture.PathId,
                static (_, arg) => {
                    arg.Deserialize(ObjectDeserializationOptions.Default);
                    var data = Texture2DConverter.ToRGBA(arg);
                    if (arg.TextureFormat.IsAlphaFirst()) {
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

                    if (arg.TextureFormat.IsBGRA()) {
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

    public static void ClearTexMemory() {
        CachedData.Clear();
    }
}
