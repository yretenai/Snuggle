using System;
using System.Collections.Concurrent;
using System.IO;
using Equilibrium.Converters;
using Equilibrium.Implementations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Entropy.Handlers {
    public static class EntropyTextureFile {
        private static ConcurrentDictionary<long, Memory<byte>> CachedData { get; } = new();

        public static byte[] LoadCachedTexture(Texture2D texture) {
            return CachedData.GetOrAdd(texture.PathId, (_, arg) => Texture2DConverter.ToRGBA(arg), texture).ToArray();
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

            if (!EntropyCore.Instance.Settings.WriteNativeTextures ||
                !SaveNative(texture, path, out var resultPath)) {
                return SavePNG(texture, path);
            }

            return resultPath;
        }

        private static string SavePNG(Texture2D texture, string path) {
            var data = new Memory<byte>(LoadCachedTexture(texture));
            path = Path.ChangeExtension(path, ".png");
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

            using var fs = File.OpenWrite(destinationPath);
            fs.SetLength(0);
            fs.Write(Texture2DConverter.ToDDS(texture));
            return true;
        }
    }
}
