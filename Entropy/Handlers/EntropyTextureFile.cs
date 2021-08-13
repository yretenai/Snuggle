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

        public static Memory<byte> LoadCachedTexture(Texture2D texture) {
            return CachedData.GetOrAdd(texture.PathId, (_, arg) => Texture2DConverter.ToRGBA(arg), texture);
        }

        public static void ClearMemory() {
            CachedData.Clear();
        }

        public static void Save(Texture2D texture, string path) {
            if (!EntropyCore.Instance.Settings.WriteNativeTextures || !SaveNative(texture, path)) {
                SavePNG(texture, path);
            }
        }

        private static void SavePNG(Texture2D texture, string path) {
            var data = LoadCachedTexture(texture);
            if (data.IsEmpty) {
                return;
            }

            var image = Image.WrapMemory<Rgba32>(data, texture.Width, texture.Height);
            image.SaveAsPng(Path.ChangeExtension(path, ".png"));
        }

        private static bool SaveNative(Texture2D texture, string path) {
            if (!texture.TextureFormat.CanSupportDDS()) {
                return false;
            }

            using var fs = File.OpenWrite(Path.ChangeExtension(path, ".dds"));
            fs.SetLength(0);
            fs.Write(Texture2DConverter.ToDDS(texture));
            return true;
        }
    }
}
