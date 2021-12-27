using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>The root nodes of a scene.</summary>
public record Scene : ChildOfRootProperty, INodeCreator {
    /// <summary>The indices of each root node.</summary>
    [JsonPropertyName("nodes")]
    public List<int> Nodes { get; set; } = new();

    public (Node Node, int Id) CreateNode(Root root) {
        var node = new Node();
        root.Nodes ??= new List<Node>();
        var id = root.Nodes.Count;
        root.Nodes.Add(node);
        Nodes.Add(id);
        return (node, id);
    }
}
