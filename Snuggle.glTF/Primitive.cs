using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
/// Geometry to be rendered with the given material.
/// </summary>
public record Primitive : Property {
    /// <summary>
    /// A plain JSON object, where each key corresponds to a mesh attribute semantic and each value is the index of the accessor containing attribute's data.
    /// </summary>
    [JsonPropertyName("attributes")]
    public Dictionary<string, int> Attributes { get; set; } = new();

    /// <summary>
    /// The index of the accessor that contains the vertex indices.
    /// When this is undefined, the primitive defines non-indexed geometry.
    /// When defined, the accessor <b>MUST</b> have `SCALAR` type and an unsigned integer component type.
    /// </summary>
    [JsonPropertyName("indices")]
    public int? Indices { get; set; }

    /// <summary>
    /// The index of the material to apply to this primitive when rendering.
    /// </summary>
    [JsonPropertyName("material")]
    public int? Material { get; set; }

    /// <summary>
    /// The topology type of primitives to render.
    /// </summary>
    [JsonPropertyName("mode")]
    public PrimitiveMode? Mode { get; set; }

    /// <summary>
    /// An array of morph targets.
    /// </summary>
    [JsonPropertyName("targets")]
    public List<Dictionary<string, int>>? Targets { get; set; }
}
