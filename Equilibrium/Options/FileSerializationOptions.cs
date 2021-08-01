using System.Text.Json;
using Equilibrium.Meta;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Options {
    [PublicAPI]
    public record FileSerializationOptions(
        int Alignment,
        long ResourceDataThreshold,
        UnityVersion TargetVersion,
        UnityGame TargetGame,
        UnitySerializedFileVersion TargetFileVersion,
        bool IsBundle,
        string ResourceSuffix) {
        public static FileSerializationOptions Default { get; } = new(8, 0, UnityVersion.MinValue, UnityGame.Default, UnitySerializedFileVersion.Invalid, true, ".resS");

        public static FileSerializationOptions FromJson(string json) {
            try {
                return JsonSerializer.Deserialize<FileSerializationOptions>(json, EquilibriumOptions.JsonOptions) ?? Default;
            } catch {
                return Default;
            }
        }

        public string ToJson() => JsonSerializer.Serialize(this, EquilibriumOptions.JsonOptions);
    }
}
