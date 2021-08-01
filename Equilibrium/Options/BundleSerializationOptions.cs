using System.Text.Json;
using Equilibrium.Meta;
using Equilibrium.Models.Bundle;
using JetBrains.Annotations;

namespace Equilibrium.Options {
    [PublicAPI]
    public record BundleSerializationOptions(
        int BlockSize,
        UnityCompressionType CompressionType,
        UnityCompressionType BlockCompressionType,
        int TargetVersion,
        UnityGame TargetGame) {
        public static BundleSerializationOptions LZMA { get; } = new(int.MaxValue, UnityCompressionType.None, UnityCompressionType.LZMA, -1, UnityGame.Default);
        public static BundleSerializationOptions LZ4 { get; } = new(0x20000, UnityCompressionType.None, UnityCompressionType.LZ4, -1, UnityGame.Default);
        public static BundleSerializationOptions Default { get; } = new(int.MaxValue, UnityCompressionType.None, UnityCompressionType.None, -1, UnityGame.Default);

        public static BundleSerializationOptions FromJson(string json) {
            try {
                return JsonSerializer.Deserialize<BundleSerializationOptions>(json, EquilibriumOptions.JsonOptions) ?? Default;
            } catch {
                return Default;
            }
        }

        public string ToJson() => JsonSerializer.Serialize(this, EquilibriumOptions.JsonOptions);
    }
}
