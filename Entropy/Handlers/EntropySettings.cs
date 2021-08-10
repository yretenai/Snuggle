﻿using System.Collections.Generic;
using System.Text.Json;
using Equilibrium.Options;

namespace Entropy.Handlers {
    public record EntropySettings(
        EquilibriumOptions Options,
        ObjectDeserializationOptions ObjectOptions,
        BundleSerializationOptions BundleOptions,
        FileSerializationOptions FileOptions,
        bool WriteNativeTextures,
        bool UseContainerPaths,
        bool GroupByType,
        string NameTemplate,
        MaterialPrimaryColor Color,
        bool LightMode) {
        private const int LatestVersion = 3;

        public List<string> RecentFiles { get; set; } = new();
        public List<string> RecentDirectories { get; set; } = new();
        public string LastSaveDirectory { get; set; } = string.Empty;

        public static EntropySettings Default { get; } =
            new(EquilibriumOptions.Default,
                ObjectDeserializationOptions.Default,
                BundleSerializationOptions.Default,
                FileSerializationOptions.Default,
                true,
                true,
                true,
                "{0}.{1:G}_{2:G}.bytes", // 0 = Name, 1 = PathId, 2 = Type
                MaterialPrimaryColor.Grey,
                false);

        public int Version { get; set; } = LatestVersion;

        public static EntropySettings FromJson(string json) {
            try {
                var settings = JsonSerializer.Deserialize<EntropySettings>(json, EquilibriumOptions.JsonOptions) ?? Default;

                if (settings.Options.NeedsMigration()) {
                    settings = settings with { Options = settings.Options.Migrate() };
                }

                if (settings.BundleOptions.NeedsMigration()) {
                    settings = settings with { BundleOptions = settings.BundleOptions.Migrate() };
                }

                if (settings.FileOptions.NeedsMigration()) {
                    settings = settings with { FileOptions = settings.FileOptions.Migrate() };
                }

                if (settings.ObjectOptions.NeedsMigration()) {
                    settings = settings with { ObjectOptions = settings.ObjectOptions.Migrate() };
                }

                return settings.NeedsMigration() ? settings.Migrate() : settings;
            } catch {
                return Default;
            }
        }

        public string ToJson() => JsonSerializer.Serialize(this, EquilibriumOptions.JsonOptions);

        public bool NeedsMigration() => Version < LatestVersion;

        public EntropySettings Migrate() {
            var settings = this;

            if (settings.Version < 2) {
                settings = settings with { RecentDirectories = new List<string>(), RecentFiles = new List<string>(), LastSaveDirectory = string.Empty, Version = 2 };
            }

            if (settings.Version < 3) {
                settings = settings with { Color = MaterialPrimaryColor.Red, LightMode = false, Version = 3 };
            }

            return settings with { Version = LatestVersion };
        }
    }
}
