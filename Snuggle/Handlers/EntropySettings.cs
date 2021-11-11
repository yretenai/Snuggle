using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Snuggle.Core.Options;

namespace Snuggle.Handlers; 

public record SnuggleSettings(
    SnuggleOptions Options,
    ObjectDeserializationOptions ObjectOptions,
    BundleSerializationOptions BundleOptions,
    FileSerializationOptions FileOptions,
    bool WriteNativeTextures,
    bool UseContainerPaths,
    bool GroupByType,
    string NameTemplate,
    bool LightMode,
    bool BubbleGameObjectsDown,
    bool BubbleGameObjectsUp,
    bool DisplayRelationshipLines) {
    private const int LatestVersion = 5;

    public List<string> RecentFiles { get; set; } = new();
    public List<string> RecentDirectories { get; set; } = new();
    public string LastSaveDirectory { get; set; } = string.Empty;
    public HashSet<RendererType> EnabledRenders { get; set; } = Enum.GetValues<RendererType>().ToHashSet();

    public static SnuggleSettings Default { get; } =
        new(SnuggleOptions.Default,
            ObjectDeserializationOptions.Default,
            BundleSerializationOptions.Default,
            FileSerializationOptions.Default,
            true,
            true,
            true,
            "{0}.{1:G}_{2:G}.bytes", // 0 = Name, 1 = PathId, 2 = Type
            false,
            true,
            true,
            true);

    public int Version { get; set; } = LatestVersion;

    public static SnuggleSettings FromJson(string json) {
        try {
            var settings = JsonSerializer.Deserialize<SnuggleSettings>(json, SnuggleOptions.JsonOptions) ?? Default;

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

    public string ToJson() => JsonSerializer.Serialize(this, SnuggleOptions.JsonOptions);

    public bool NeedsMigration() => Version < LatestVersion;

    public SnuggleSettings Migrate() {
        var settings = this;

        if (settings.Version < 2) {
            settings = settings with { RecentDirectories = new List<string>(), RecentFiles = new List<string>(), LastSaveDirectory = string.Empty, Version = 2 };
        }

        if (settings.Version < 3) {
            settings = settings with { LightMode = false, Version = 3 };
        }

        if (settings.Version < 4) {
            settings = settings with { EnabledRenders = Enum.GetValues<RendererType>().ToHashSet(), BubbleGameObjectsDown = true, BubbleGameObjectsUp = true };
        }

        if (settings.Version < 5) {
            settings = settings with { DisplayRelationshipLines = true };
        }

        return settings with { Version = LatestVersion };
    }
}
