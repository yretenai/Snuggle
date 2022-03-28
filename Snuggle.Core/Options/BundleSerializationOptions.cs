using System.Text.Json;
using System.Text.Json.Serialization;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Bundle;

namespace Snuggle.Core.Options;

public record BundleSerializationOptions(int BlockSize, UnityCompressionType CompressionType, UnityCompressionType BlockCompressionType) {
    private const int LatestVersion = 1;
    public static BundleSerializationOptions LZMA { get; } = new(int.MaxValue, UnityCompressionType.None, UnityCompressionType.LZMA);
    public static BundleSerializationOptions LZ4 { get; } = new(0x20000, UnityCompressionType.None, UnityCompressionType.LZ4);
    public static BundleSerializationOptions Default { get; } = new(int.MaxValue, UnityCompressionType.None, UnityCompressionType.None);

    [JsonIgnore]
    public int TargetFormatVersion { get; init; } = -1;

    [JsonIgnore]
    public UnityGame TargetGame { get; init; } = UnityGame.Default;

    public int Version { get; set; } = LatestVersion;

    public BundleSerializationOptions MutateWithBundle(Bundle bundle) => this with { TargetFormatVersion = bundle.Header.FormatVersion, TargetGame = bundle.Options.Game };

    public static BundleSerializationOptions FromJson(string json) {
        try {
            var options = JsonSerializer.Deserialize<BundleSerializationOptions>(json, SnuggleCoreOptions.JsonOptions) ?? Default;
            return options.NeedsMigration() ? options.Migrate() : options;
        } catch {
            return Default;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, SnuggleCoreOptions.JsonOptions);
    public bool NeedsMigration() => Version < LatestVersion;

    public BundleSerializationOptions Migrate() => this with { Version = LatestVersion };
}
