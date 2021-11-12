using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Serialization;

namespace Snuggle.Core.Options;

[PublicAPI]
public record FileSerializationOptions(int Alignment, long ResourceDataThreshold, string ResourceSuffix, string BundleTemplate) { // 0 = Name
    private const int LatestVersion = 1;
    public static FileSerializationOptions Default { get; } = new(8, 0, ".resS", "archive:/{0}/");

    [JsonIgnore]
    public UnityVersion TargetVersion { get; init; } = UnityVersion.MinValue;

    [JsonIgnore]
    public UnityGame TargetGame { get; init; } = UnityGame.Default;

    [JsonIgnore]
    public UnitySerializedFileVersion TargetFileVersion { get; init; } = UnitySerializedFileVersion.Invalid;

    [JsonIgnore]
    public bool IsBundle { get; init; } = true;

    public int Version { get; set; } = LatestVersion;

    public FileSerializationOptions MutateWithSerializedFile(SerializedFile serializedFile) => this with { TargetVersion = serializedFile.Version, TargetGame = serializedFile.Options.Game, TargetFileVersion = serializedFile.Header.FileVersion };

    public static FileSerializationOptions FromJson(string json) {
        try {
            var options = JsonSerializer.Deserialize<FileSerializationOptions>(json, SnuggleCoreOptions.JsonOptions) ?? Default;
            return options.NeedsMigration() ? options.Migrate() : options;
        } catch {
            return Default;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, SnuggleCoreOptions.JsonOptions);
    public bool NeedsMigration() => Version < LatestVersion;

    public FileSerializationOptions Migrate() => this with { Version = LatestVersion };
}
