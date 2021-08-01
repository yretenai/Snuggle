using System.Text.Json;
using System.Text.Json.Serialization;
using Equilibrium.Interfaces;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Options {
    [PublicAPI]
    public record EquilibriumOptions(
        bool CacheData,
        bool CacheDataIfLZMA, // this literally takes two years, you want it to be enabled.
        UnityGame Game) {
        [JsonIgnore]
        public IStatusReporter? Reporter { get; set; }

        public static EquilibriumOptions Default { get; } = new(false, true, UnityGame.Default);

        public static JsonSerializerOptions JsonOptions { get; } = new() {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            AllowTrailingCommas = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters = {
                new JsonStringEnumConverter(),
            },
        };

        public static EquilibriumOptions FromJson(string json) {
            try {
                return JsonSerializer.Deserialize<EquilibriumOptions>(json, JsonOptions) ?? Default;
            } catch {
                return Default;
            }
        }

        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
    }
}
