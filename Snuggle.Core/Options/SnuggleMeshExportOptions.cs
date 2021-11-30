﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Snuggle.Core.Options;

public record SnuggleMeshExportOptions(
    [Description("Find game objects by following the hierarchy tree downwards")] bool FindGameObjectDescendants,
    [Description("Find game objects by following the hierarchy tree upwards")] bool FindGameObjectParents,
    [Description("Render game object hierarchy relationship lines")] bool DisplayRelationshipLines,
    [Description("Render mesh wireframe")] bool DisplayWireframe,
    [Description("Write Vertex Colors to GLTF")] bool WriteVertexColors,
    [Description("Write Material Textures")] bool WriteTexture,
    [Description("Write Material JSON files")] bool WriteMaterial,
    [Description("Write Morph Targets to GLTF")] bool WriteMorphs) {
    private const int LatestVersion = 1;

    public static SnuggleMeshExportOptions Default { get; } = new(
        true,
        false,
        true,
        false,
        true,
        true,
        true,
        true);

    public HashSet<RendererType> EnabledRenders { get; set; } = Enum.GetValues<RendererType>().ToHashSet();
    public int Version { get; set; } = LatestVersion;
    public bool NeedsMigration() => Version < LatestVersion;
    public SnuggleMeshExportOptions Migrate() => this with { Version = LatestVersion };
}