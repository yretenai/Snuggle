using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
/// The root nodes of a scene.
/// </summary>
public record Scene : ChildOfRootProperty {
    /// <summary>
    /// The indices of each root node.
    /// </summary>
    [JsonPropertyName("nodes")]
    public List<int> Nodes { get; set; } = new();
}
