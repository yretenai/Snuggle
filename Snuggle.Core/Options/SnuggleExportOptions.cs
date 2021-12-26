using System.ComponentModel;
using JetBrains.Annotations;
using Snuggle.Core.Implementations;

namespace Snuggle.Core.Options;

[PublicAPI]
public record SnuggleExportOptions([Description("Writes Native 3D textures such as DDS instead of converting them to PNG or TIF")] bool WriteNativeTextures, [Description(SnuggleExportOptions.PathTemplateDescription)] string PathTemplate, [Description(SnuggleExportOptions.PathTemplateDescription)] string ContainerlessPathTemplate, [Description("Use DirectXTex for converting textures")] bool UseDirectTex, [Description("Only display and export objects with CAB paths")] bool OnlyWithCABPath, [Description("Use New glTF Logic")] bool UseNewGLTFExporter) {
    private const string PathTemplateDescription = @"Output Path Template
Available variables:
    Id - The Path ID of the object.
    Type - The Class ID of the object.
    Size - Byte size of the object.
    Container - The CAB Path of the object.
    Name - The name of the object.
    ContainerOrName - The path of the object if it is present, otherwise the name.
    ContainerOrNameWithExt - The path of the object if it is present, otherwise the name; suffixes extension.
    ContainerOrNameWithoutExt - The path of the object if it is present, otherwise the name; removes extension.
    Ext - The Extension of the object.
    Company - The company name listed in the PlayerSettings (if present.)
    Organization - The Organization Id listed in the PlayerSettings (if present.)
    Project - The project name listed in the PlayerSettings (if present.)
    Product - The product name listed in the PlayerSettings (if present.)
    ProductOrProject - The product name listed in the PlayerSettings, otherwise project name (if present.)
    Version - The bundle version listed in the PlayerSettings (if present.)";

    public const string DefaultPathTemplate = "{ProductOrProject}/{Version}/{ContainerOrNameWithoutExt}_{Id}.{Ext}";
    public const string DefaultContainerlessPathTemplate = "{ProductOrProject}/{Version}/__unknown/{Type}/{Name}_{Id}.{Ext}";

    private const int LatestVersion = 6;
    public int Version { get; set; } = LatestVersion;

    public static SnuggleExportOptions Default { get; } = new(true, DefaultPathTemplate, DefaultContainerlessPathTemplate, true, false, false);

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
            settings = settings with { PathTemplate = DefaultPathTemplate };
        }

        if (Version < 5) {
            settings = settings with { ContainerlessPathTemplate = DefaultContainerlessPathTemplate };
        }

        if (Version < 6) {
            settings = settings with { UseNewGLTFExporter = true };
        }

        return settings with { Version = LatestVersion };
    }

    public string DecidePathTemplate(SerializedObject serializedObject) {
        return serializedObject.HasContainerPath ? PathTemplate : ContainerlessPathTemplate;
    }
}
