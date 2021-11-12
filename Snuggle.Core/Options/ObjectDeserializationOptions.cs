using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Snuggle.Core.Options;

public delegate (string Path, SnuggleOptions Options) RequestAssemblyPath(string assemblyName);

[PublicAPI]
public record ObjectDeserializationOptions {
    private const int LatestVersion = 1;

    [JsonIgnore]
    public RequestAssemblyPath? RequestAssemblyCallback { get; set; }

    public static ObjectDeserializationOptions Default { get; } = new();
    public int Version { get; set; } = LatestVersion;

    public static ObjectDeserializationOptions FromJson(string json) {
        try {
            var options = JsonSerializer.Deserialize<ObjectDeserializationOptions>(json, SnuggleOptions.JsonOptions) ?? Default;
            return options.NeedsMigration() ? options.Migrate() : options;
        } catch {
            return Default;
        }
    }

    public bool NeedsMigration() => Version < LatestVersion;

    public ObjectDeserializationOptions Migrate() => this with { Version = LatestVersion };
}
