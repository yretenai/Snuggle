using System;
using System.Collections.Generic;
using System.Linq;
using Equilibrium.Implementations;
using Equilibrium.Models;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;
using Mono.Cecil;

namespace Equilibrium.Meta {
    [PublicAPI]
    public record ObjectNode(
        string Name,
        string TypeName,
        Type? Type,
        int Size,
        bool IsAligned) {
        public static ObjectNode Empty { get; } = new(string.Empty, string.Empty, null, 0, false);
        public List<ObjectNode> Properties { get; set; } = new();


        public static ObjectNode FromUnityTypeTree(UnityTypeTree tree) {
            return tree.Nodes.Length == 0 ? Empty : FromUnityTypeTreeNode(tree.Nodes[0], tree.Nodes.Skip(1).ToArray());
        }

        public static ObjectNode FromUnityTypeTreeNode(UnityTypeTreeNode node, UnityTypeTreeNode[] nodes) {
            var type = DetermineTypeFromUnityType(node);
            var objectNode = new ObjectNode(node.Name, node.Type, type, node.Size, node.Flags.HasFlag(UnityTypeTreeFlags.Align));
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
            throw new NotImplementedException();
        }
        
        private static Type? DetermineTypeFromUnityType(UnityTypeTreeNode node) {
            var typeName = node.Type.ToLower();
            Type? type;
            if (typeName.StartsWith("pptr<")) {
                type = typeof(PPtr<SerializedObject>);
            } else {
                type = typeName switch {
                    "char" => typeof(char),
                    "bool" => typeof(bool),
                    "boolean" => typeof(bool),
                    "uint8" => node.Flags.HasFlag(UnityTypeTreeFlags.Boolean) ? typeof(bool) : typeof(byte),
                    "sint8" => typeof(sbyte),
                    "sint16" => typeof(short),
                    "uint16" => typeof(ushort),
                    "sint32" => typeof(int),
                    "uint32" => typeof(uint),
                    "sint64" => typeof(long),
                    "uint64" => typeof(ulong),
                    "short" => typeof(short),
                    "unsigned short" => typeof(ushort),
                    "int" => typeof(int),
                    "unsigned int" => typeof(uint),
                    "type*" => typeof(uint),
                    "long long" => typeof(long),
                    "unsigned long long" => typeof(ulong),
                    "filesize" => typeof(ulong),
                    "string" => typeof(string),
                    "double" => typeof(double),
                    "map" => typeof(Dictionary<object, object>),
                    "typelessdata" => typeof(byte[]),
                    "array" => typeof(List<object>),
                    "vector" => typeof(List<object>),
                    "float" => typeof(float),
                    "guid" => typeof(Guid),
                    "hash128" => typeof(byte[]),
                    _ => null,
                };
            }

            if (type == null) {
                return null;
            }

            if (node.ArrayKind != UnityTypeArrayKind.None) {
                type = type.MakeArrayType();
            }

            return type;
        }
    }
}
