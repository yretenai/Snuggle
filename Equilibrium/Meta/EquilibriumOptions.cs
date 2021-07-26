using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Meta {
    [PublicAPI]
    public record EquilibriumOptions(
        bool CacheData,
        UnityGame Game) {
        [JsonIgnore]
        public IStatusReporter? Reporter { get; set; }

        public static EquilibriumOptions Default { get; } = new(false, UnityGame.Default);

        private static JsonSerializerOptions SerializerOptions { get; } = new() {
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
                return JsonSerializer.Deserialize<EquilibriumOptions>(json, SerializerOptions) ?? Default;
            } catch {
                return Default;
            }
        }

        public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);
    }
}
