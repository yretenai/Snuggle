﻿using System.Text.Json;
using JetBrains.Annotations;

namespace Equilibrium.Options {
    [PublicAPI]
    public record FileSerializationOptions(
        int Alignment,
        long ResourceDataThreshold) {
        public static FileSerializationOptions Default { get; } = new(8, 0);

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