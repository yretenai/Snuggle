using System.Text.Json;
using System.Text.Json.Serialization;
using Equilibrium.Meta.Interfaces;
using JetBrains.Annotations;

namespace Equilibrium.Meta.Options {
    [PublicAPI]
    public record EquilibriumOptions(
        bool CacheData,
        UnityGame Game) {
        [JsonIgnore]
        public IStatusReporter? Reporter { get; set; }

        public static EquilibriumOptions Default { get; } = new(false, UnityGame.Default);

        internal static JsonSerializerOptions JsonOptions { get; } = new() {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            AllowTrailingCommas = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters = {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
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
