﻿using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Meta {
    [PublicAPI]
    public record EquilibriumOptions(
        bool CacheData) {
        [JsonIgnore]
        public IStatusReporter? Reporter { get; set; }

        public static EquilibriumOptions Default { get; } = new(false);

        public static EquilibriumOptions FromJson(string json) {
            try {
                return JsonSerializer.Deserialize<EquilibriumOptions>(json) ?? Default;
            } catch {
                return Default;
            }
        }

        public string ToJson() => JsonSerializer.Serialize(this);
    }
}