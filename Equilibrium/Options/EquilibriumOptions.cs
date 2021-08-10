using System.Text.Json;
using System.Text.Json.Serialization;
using Equilibrium.Interfaces;
using Equilibrium.Logging;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Options {
    [PublicAPI]
    public record EquilibriumOptions(
        bool CacheData,
        bool CacheDataIfLZMA, // this literally takes two years, you want it to be enabled.
        bool LoadOnDemand,
        UnityGame Game) {
        private const int LatestVersion = 3;
        public int Version { get; set; } = LatestVersion;

        [JsonIgnore]
        public IStatusReporter? Reporter { get; set; }

        [JsonIgnore]
        public ILogger Logger { get; set; } = DebugLogger.Instance;

        public static EquilibriumOptions Default { get; } = new(false, true, false, UnityGame.Default);

        public static JsonSerializerOptions JsonOptions { get; } = new() {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters = {
                new JsonStringEnumConverter(),
            },
        };

        public static EquilibriumOptions FromJson(string json) {
            try {
                var options = JsonSerializer.Deserialize<EquilibriumOptions>(json, JsonOptions) ?? Default;
                return options.NeedsMigration() ? options.Migrate() : options;
            } catch {
                return Default;
            }
        }

        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        public bool NeedsMigration() => Version < LatestVersion;

        public EquilibriumOptions Migrate() {
            var options = this;
            if (options.Version <= 1) {
                options = options with { CacheDataIfLZMA = true, Version = 2 };
            }

            if (options.Version == 2) {
                options = options with { LoadOnDemand = false, Version = 3 };
            }

            return options;
        }
    }
}
