using System.ComponentModel;
using JetBrains.Annotations;

namespace Snuggle.Core.Options;

[PublicAPI]
public record SnuggleExportOptions([Description("Writes Native 3D textures such as DDS instead of converting them to PNG or TIF")] bool WriteNativeTextures, [Description(SnuggleExportOptions.PathTemplateDescription)] string PathTemplate, [Description("Use DirectXTex for converting textures")] bool UseDirectTex, [Description("Only display and export objects with CAB paths")] bool OnlyWithCABPath) {
    private const string PathTemplateDescription = @"Output Path Template
Available variables:
    Id - The Path ID of the object.
    Type - The Class ID of the object.
    Size - Byte size of the object.
    Container - The CAB Path of the object.
    Ext - The Extension of the object.
    Name - The name of the object.";
    
    private const int LatestVersion = 4;
    public int Version { get; set; } = LatestVersion;

    public static SnuggleExportOptions Default { get; } = new(true, "{Type}/{Container}/{Id}_{Name}.{Ext}", true, false);

    public bool NeedsMigration() => Version < LatestVersion;

    public SnuggleExportOptions Migrate() {
        var settings = this;

        if (Version < 2) {
            settings = settings with { UseDirectTex = true };
        }

        if (Version < 3) {
            settings = settings with { OnlyWithCABPath = false };
        }

        if (Version < 4) {
            settings = settings with { PathTemplate = "{Type}/{Container}/{Id}_{Name}.{Ext}" };
        }

        return settings with { Version = LatestVersion };
    }
}
