using System.ComponentModel;
using JetBrains.Annotations;

namespace Snuggle.Core.Options;

[PublicAPI]
public record SnuggleExportOptions([Description("Writes Native 3D textures such as DDS instead of converting them to PNG or TIF")] bool WriteNativeTextures, [Description("Uses CAB container paths")] bool UseContainerPaths, [Description("Group By Class Id")] bool GroupByType, string NameTemplate, [Description("Use DirectXTex for converting textures")] bool UseDirectTex, [Description("Only display and export objects with CAB paths")] bool OnlyWithCABPath) {
    private const int LatestVersion = 3;
    public int Version { get; set; } = LatestVersion;

    public static SnuggleExportOptions Default { get; } = new(true, true, true, "{0}.{1:G}_{2:G}.bytes", true, false);

    public bool NeedsMigration() => Version < LatestVersion;

    public SnuggleExportOptions Migrate() {
        var settings = this;

        if (Version < 2) {
            settings = settings with { UseDirectTex = true };
        }

        if (Version < 3) {
            settings = settings with { OnlyWithCABPath = false };
        }

        return settings with { Version = LatestVersion };
    }
}
