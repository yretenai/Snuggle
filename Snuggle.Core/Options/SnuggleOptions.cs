using System.Collections.Generic;
using System.Text.Json;

namespace Snuggle.Core.Options;

public record SnuggleOptions(SnuggleCoreOptions Options, ObjectDeserializationOptions ObjectOptions, BundleSerializationOptions BundleOptions, FileSerializationOptions FileOptions, bool LightMode) {
    private const int LatestVersion = 8;

    public SnuggleExportOptions ExportOptions { get; set; } = SnuggleExportOptions.Default;
    public SnuggleMeshExportOptions MeshExportOptions { get; set; } = SnuggleMeshExportOptions.Default;

    public List<string> RecentFiles { get; set; } = new();
    public List<string> RecentDirectories { get; set; } = new();
    public string LastSaveDirectory { get; set; } = string.Empty;

    public static SnuggleOptions Default { get; } = new(SnuggleCoreOptions.Default, ObjectDeserializationOptions.Default, BundleSerializationOptions.Default, FileSerializationOptions.Default, false) { ExportOptions = SnuggleExportOptions.Default, MeshExportOptions = SnuggleMeshExportOptions.Default };

    public int Version { get; set; } = LatestVersion;

    public static SnuggleOptions FromJson(string json) {
        try {
            var settings = JsonSerializer.Deserialize<SnuggleOptions>(json, SnuggleCoreOptions.JsonOptions) ?? Default;

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

            if (settings.ExportOptions.NeedsMigration()) {
                settings = settings with { ExportOptions = settings.ExportOptions.Migrate() };
            }

            if (settings.MeshExportOptions.NeedsMigration()) {
                settings = settings with { MeshExportOptions = settings.MeshExportOptions.Migrate() };
            }

            return settings.NeedsMigration() ? settings.Migrate() : settings;
        } catch {
            return Default;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, SnuggleCoreOptions.JsonOptions);

    public bool NeedsMigration() => Version < LatestVersion;

    public SnuggleOptions Migrate() {
        var settings = this;

        if (settings.Version < 2) {
            settings = settings with { RecentDirectories = new List<string>(), RecentFiles = new List<string>(), LastSaveDirectory = string.Empty, Version = 2 };
        }

        if (settings.Version < 3) {
            settings = settings with { LightMode = false, Version = 3 };
        }

        // if (settings.Version < 4) {
        //     settings = settings with { EnabledRenders = Enum.GetValues<RendererType>().ToHashSet(), BubbleGameObjectsDown = true, BubbleGameObjectsUp = true };
        // }
        //
        // if (settings.Version < 5) {
        //     settings = settings with { DisplayRelationshipLines = true };
        // }
        //
        // if (settings.Version < 6) {
        //     settings = settings with { DisplayWireframe = false };
        // }
        //
        // if (settings.Version < 7) {
        //     settings = settings with { WriteVertexColors = true };
        // }

        if (settings.Version < 8) {
            settings = settings with { MeshExportOptions = SnuggleMeshExportOptions.Default, ExportOptions = SnuggleExportOptions.Default };
        }

        return settings with { Version = LatestVersion };
    }
}
