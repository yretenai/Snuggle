using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
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
    private const int LatestVersion = 4;
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
        Converters = { new JsonStringEnumConverter() },
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
            options = options with { CacheDataIfLZMA = true, Version = 2 };
        }

        if (options.Version == 2) {
            options = options with { LoadOnDemand = false, Version = 3 };
        }

        if (options.Version == 3) {
            options = options with { GameOptions = UnityGameOptions.Default, Version = 4 };
        }

        options.GameOptions = options.GameOptions.Migrate();

        return options;
    }
}
