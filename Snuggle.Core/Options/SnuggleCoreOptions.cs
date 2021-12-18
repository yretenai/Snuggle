using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Snuggle.Core.Extensions;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Logging;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Options;

[PublicAPI]
public record SnuggleCoreOptions(
    bool CacheData,
    bool CacheDataIfLZMA, // this literally takes two years, you want it to be enabled.
    bool LoadOnDemand,
    UnityGame Game) {
    public HashSet<string> IgnoreClassIds { get; set; } = new();
    private const int LatestVersion = 5;
    public int Version { get; set; } = LatestVersion;
    public UnityGameOptions GameOptions { get; set; } = UnityGameOptions.Default;

    [JsonIgnore]
    public IStatusReporter? Reporter { get; set; }

    [JsonIgnore]
    public ILogger Logger { get; set; } = DebugLogger.Instance;

    public static SnuggleCoreOptions Default { get; } = new(false, true, false, UnityGame.Default);

    public static JsonSerializerOptions JsonOptions { get; } = new() {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
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

        if (options.Version <= 3) {
            options = options with { GameOptions = UnityGameOptions.Default };
        }

        if (options.Version <= 4) {
            options = options with { IgnoreClassIds = new HashSet<string>() };
        }

        options.GameOptions = options.GameOptions.Migrate();

        return options with { Version = LatestVersion };
    }
}
