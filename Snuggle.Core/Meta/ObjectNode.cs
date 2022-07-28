using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Snuggle.Core.Models.Serialization;

namespace Snuggle.Core.Meta;

public record ObjectNode(string Name, string TypeName, int Size, UnityTransferMetaFlags Meta, UnityTransferTypeFlags TypeFlags, Dictionary<int, string>? EnumValues) {
    public static ObjectNode Empty { get; } = new(string.Empty, string.Empty, 0, UnityTransferMetaFlags.None, UnityTransferTypeFlags.None, null);
    public List<ObjectNode> Properties { get; init; } = new();

    public static ObjectNode FromUnityTypeTree(UnityTypeTree typeTree) => typeTree.Nodes.Length == 0 ? Empty : FromUnityTypeTreeNode(typeTree.Nodes[0], typeTree.Nodes.Skip(1).ToArray());

    public static ObjectNode FromUnityTypeTreeNode(UnityTypeTreeNode rootNode, UnityTypeTreeNode[] subNodes) {
        var objectNode = new ObjectNode(rootNode.Name, rootNode.Type, rootNode.Size, rootNode.Meta,  rootNode.TypeFlags, null);
        for (var i = 0; i < subNodes.Length; ++i) {
            var subNode = subNodes[i];
            if (rootNode.Level >= subNode.Level) {
                break;
            }

            if (subNode.Level == rootNode.Level + 1) {
                objectNode.Properties.Add(FromUnityTypeTreeNode(subNode, subNodes.Skip(i + 1).TakeWhile(x => x.Level > rootNode.Level + 1).ToArray()));
            }
        }

        return objectNode;
    }

    public static ObjectNode FromCecil(TypeDefinition typeDefinition) {
        var converter = new TypeDefinitionConverter(typeDefinition, typeDefinition);
        var objectNode = new ObjectNode("Base", "MonoBehaviour", -1, UnityTransferMetaFlags.None, UnityTransferTypeFlags.None, null) {
            Properties = new List<ObjectNode> { // these all get skipped.
                new("m_GameObject", "PPtr<GameObject>", 12, UnityTransferMetaFlags.None, UnityTransferTypeFlags.None, null),
                new("m_Enabled", "UInt8", 1, UnityTransferMetaFlags.AlignBytes, UnityTransferTypeFlags.None, null),
                new("m_Script", "PPtr<MonoScript>", 12, UnityTransferMetaFlags.None, UnityTransferTypeFlags.None, null),
                new("m_Name", "string", -1, UnityTransferMetaFlags.AlignBytes, UnityTransferTypeFlags.None, null),
            },
        };
        objectNode.Properties.AddRange(converter.ConvertToObjectNodes());
        return objectNode;
    }
}
