using System.ComponentModel;
using Snuggle.Core.Interfaces;

namespace Snuggle.Core.Options;

public record SnuggleExportOptions(
    [Description("Writes Native 3D textures such as DDS instead of converting them to PNG or TIF")]
    bool WriteNativeTextures,
    [Description(SnuggleExportOptions.PathTemplateDescription)]
    string PathTemplate,
    [Description(SnuggleExportOptions.PathTemplateDescription)]
    string ContainerlessPathTemplate,
    [Description("Use AssetStudio's Texture2DDecoder for converting textures")]
    bool UseTextureDecoder,
    [Description("Only display and export objects with CAB paths")]
    bool OnlyWithCABPath,
    [Description("Keep audio samples in their native format")]
    bool WriteNativeAudio) {
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
    Version - The bundle version listed in the PlayerSettings (if present.)
    Tag - The Serialized File tag, usually it's filename.
    Bundle - The Bundle tag, usually it's filename.
    BundleOrTag - The bundle tag if present, otherwise the tag.
    Script - The full class name if present on a MonoBehaviour";

    public const string DefaultPathTemplate = "{ProductOrProject}/{Version}/{ContainerOrNameWithoutExt}_{Id}.{Ext}";
    public const string DefaultContainerlessPathTemplate = "{ProductOrProject}/{Version}/{Tag}/__unknown/{Type}/{Name}_{Id}.{Ext}";

    private const int LatestVersion = 10;
    public int Version { get; init; } = LatestVersion;

    public static SnuggleExportOptions Default { get; } = new(false, DefaultPathTemplate, DefaultContainerlessPathTemplate, false, false, true);

    public bool NeedsMigration() => Version < LatestVersion;

    public SnuggleExportOptions Migrate() {
        var settings = this;

        // Version 2 added UseDirectTex

        if (Version < 3) {
            settings = settings with { OnlyWithCABPath = false };
        }

        if (Version < 4) {
            settings = settings with { PathTemplate = DefaultPathTemplate };
        }

        if (Version < 5) {
            settings = settings with { ContainerlessPathTemplate = DefaultContainerlessPathTemplate };
        }

        // Version 6 added UseNewGLTFExporter,
        // Version 7 removed UseNewGLTFExporter, so the migration is no longer required.

        if (Version < 8) {
            settings = settings with { WriteNativeAudio = true };
        }

        if (Version < 9) {
            settings = settings with { UseTextureDecoder = true };
        }

        // Version 10 removed UseDirectTex

        return settings with { Version = LatestVersion };
    }

    public string DecidePathTemplate(ISerializedObject serializedObject) => serializedObject.HasContainerPath ? PathTemplate : ContainerlessPathTemplate;
}
