using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;

namespace Snuggle.Core.Options;

[PublicAPI]
public record SnuggleMeshExportOptions(
    [Description("Find game objects by following the hierarchy tree downwards")] bool FindGameObjectDescendants,
    [Description("Find game objects by following the hierarchy tree upwards")] bool FindGameObjectParents,
    [Description("Render game object hierarchy relationship lines")] bool DisplayRelationshipLines,
    [Description("Render mesh wireframe")] bool DisplayWireframe,
    [Description("Write Vertex Colors to GLTF")] bool WriteVertexColors,
    [Description("Write Material Textures")] bool WriteTexture,
    [Description("Write Material JSON files")] bool WriteMaterial,
    [Description("Write Morph Targets to GLTF")] bool WriteMorphs,
    [Description("Mirrors the X coordinate for positions")] bool MirrorXPosition,
    [Description("Mirrors the X coordinate for normals")] bool MirrorXNormal,
    [Description("Mirrors the X coordinate for tangents")] bool MirrorXTangent) {
    private const int LatestVersion = 4;

    public static SnuggleMeshExportOptions Default { get; } = new(
        true,
        false,
        true,
        false,
        true,
        true,
        true,
        true,
        true,
        true,
        true);

    public HashSet<RendererType> EnabledRenders { get; set; } = Enum.GetValues<RendererType>().ToHashSet();
    public int Version { get; set; } = LatestVersion;
    public bool NeedsMigration() => Version < LatestVersion;

    public SnuggleMeshExportOptions Migrate() {
        var settings = this;

        if (settings.Version < 2) {
            settings.EnabledRenders.Add(RendererType.Sprite);
        }

        if (settings.Version < 3) {
            settings.EnabledRenders.Add(RendererType.Audio);
        }
        
        if (settings.Version < 4) {
            settings = settings with { MirrorXPosition = true, MirrorXNormal = true, MirrorXTangent = true };
        }

        return settings with { Version = LatestVersion };
    }
}
