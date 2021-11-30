using System.ComponentModel;

namespace Snuggle.Core.Options;

public record SnuggleExportOptions([Description("Writes Native 3D textures such as DDS instead of converting them to PNG or TIF")] bool WriteNativeTextures, [Description("Uses CAB container paths")] bool UseContainerPaths, [Description("Group By Class Id")] bool GroupByType, string NameTemplate) {
    private const int LatestVersion = 1;
    public int Version { get; set; } = LatestVersion;

    public static SnuggleExportOptions Default { get; } = new(true, true, true, "{0}.{1:G}_{2:G}.bytes");

    public bool NeedsMigration() => Version < LatestVersion;
    public SnuggleExportOptions Migrate() => this with { Version = LatestVersion };
}
