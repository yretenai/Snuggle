using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Options {
    public delegate (string Path, EquilibriumOptions Options) RequestAssemblyPath(string assemblyName);

    [PublicAPI]
    public record ObjectDeserializationOptions {
        [JsonIgnore]
        public RequestAssemblyPath? RequestAssemblyCallback { get; set; }

        public static ObjectDeserializationOptions Default { get; } = new();

        public static ObjectDeserializationOptions FromJson(string json) {
            try {
                return JsonSerializer.Deserialize<ObjectDeserializationOptions>(json, EquilibriumOptions.JsonOptions) ?? Default;
            } catch {
                return Default;
            }
        }

        public string ToJson() => JsonSerializer.Serialize(this, EquilibriumOptions.JsonOptions);
    }
}
