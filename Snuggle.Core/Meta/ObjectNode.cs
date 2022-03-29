using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Snuggle.Core.Models.Serialization;

namespace Snuggle.Core.Meta;

public record ObjectNode(string Name, string TypeName, int Size, bool IsAligned, bool IsBoolean) {
    public static ObjectNode Empty { get; } = new(string.Empty, string.Empty, 0, false, false);
    public List<ObjectNode> Properties { get; init; } = new();

    public static ObjectNode FromUnityTypeTree(UnityTypeTree tree) => tree.Nodes.Length == 0 ? Empty : FromUnityTypeTreeNode(tree.Nodes[0], tree.Nodes.Skip(1).ToArray());

    public static ObjectNode FromUnityTypeTreeNode(UnityTypeTreeNode node, UnityTypeTreeNode[] nodes) {
        var objectNode = new ObjectNode(node.Name, node.Type, node.Size, node.Flags.HasFlag(UnityTypeTreeFlags.AlignValue), node.Flags.HasFlag(UnityTypeTreeFlags.Boolean));
        for (var i = 0; i < nodes.Length; ++i) {
            var subNode = nodes[i];
            if (node.Level >= subNode.Level) {
                break;
            }

            if (subNode.Level == node.Level + 1) {
                objectNode.Properties.Add(FromUnityTypeTreeNode(subNode, nodes.Skip(i + 1).TakeWhile(x => x.Level > node.Level + 1).ToArray()));
            }
        }

        return objectNode;
    }

    public static ObjectNode FromCecil(TypeDefinition type) {
        var converter = new TypeDefinitionConverter(type);
        var objectNode = new ObjectNode("Base", "MonoBehavior", -1, false, false) {
            Properties = new List<ObjectNode> { // these all get skipped.
                new("m_GameObject", "PPtr<GameObject>", 12, false, false),
                new("m_Enabled", "UInt8", 1, true, true),
                new("m_Script", "PPtr<MonoScript>", 12, false, false),
                new("m_Name", "string", -1, false, false),
            },
        };
        objectNode.Properties.AddRange(converter.ConvertToObjectNodes());
        return objectNode;
    }
}
