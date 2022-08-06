using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Snuggle.Core.Extensions;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Options;

public record SnuggleCoreOptions(
    bool CacheData,
    bool CacheDataIfLZMA, // this literally takes two years, you want it to be enabled.
    bool LoadOnDemand,
    string? CacheDirectory,
    UnityGame Game) {
    private const int LatestVersion = 7;
    public HashSet<string> IgnoreClassIds { get; set; } = new();
    public int Version { get; set; } = LatestVersion;

    [JsonIgnore]
    public IStatusReporter? Reporter { get; set; }

    public static SnuggleCoreOptions Default { get; } = new(false, true, false, "Cache", UnityGame.Default);

    public static JsonSerializerOptions JsonOptions { get; } = new() {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
        Converters = { new JsonStringEnumConverter(), new JsonMemoryConverterFactory() },
    };

    public static SnuggleCoreOptions FromJson(string json) {
        try {
            var options = JsonSerializer.Deserialize<SnuggleCoreOptions>(json, JsonOptions) ?? Default;
            return options.NeedsMigration() ? options.Migrate() : options;
        } catch {
            return Default;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public bool NeedsMigration() => Version < LatestVersion;

    public SnuggleCoreOptions Migrate() {
        var options = this;
        if (options.Version <= 1) {
            options = options with { CacheDataIfLZMA = true };
        }

        if (options.Version <= 2) {
            options = options with { LoadOnDemand = false };
        }

        if (options.Version <= 4) {
            options = options with { IgnoreClassIds = new HashSet<string>() };
        }

        if (options.Version <= 5) {
            options = options with { CacheDirectory = "Cache" };
        }
        
        // Version 6 added GameOptions
        // Version 7 removed GameOptions

        return options with { Version = LatestVersion };
    }
}
