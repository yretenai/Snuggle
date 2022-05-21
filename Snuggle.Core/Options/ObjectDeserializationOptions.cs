using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snuggle.Core.Options;

public delegate (string? Path, SnuggleCoreOptions? Options) RequestAssemblyPath(string assemblyName);

public record ObjectDeserializationOptions(
    [Description("Use Unity Type Tree for MonoBehavior deserialization if present")]
    bool UseTypeTree) {
    private const int LatestVersion = 2;

    [JsonIgnore]
    public RequestAssemblyPath? RequestAssemblyCallback { get; set; }

    public static ObjectDeserializationOptions Default { get; } = new(true);
    public int Version { get; set; } = LatestVersion;

    public static ObjectDeserializationOptions FromJson(string json) {
        try {
            var options = JsonSerializer.Deserialize<ObjectDeserializationOptions>(json, SnuggleCoreOptions.JsonOptions) ?? Default;
            return options.NeedsMigration() ? options.Migrate() : options;
        } catch {
            return Default;
        }
    }

    public bool NeedsMigration() => Version < LatestVersion;

    public ObjectDeserializationOptions Migrate() {
        var options = this;
        if (options.Version <= 1) {
            options = options with { UseTypeTree = true };
        }
        return options with { Version = LatestVersion };
    }
}
