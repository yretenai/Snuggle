using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.CecilTools;
using Unity.SerializationLogic;

namespace Snuggle.Core.Meta;

public class TypeDefinitionConverter {
    private readonly TypeDefinition TypeDef;
    private readonly TypeResolver TypeResolver;

    public TypeDefinitionConverter(TypeDefinition typeDef) {
        TypeDef = typeDef;
        TypeResolver = new TypeResolver(null);
    }

    public List<ObjectNode> ConvertToObjectNodes() {
        var nodes = new List<ObjectNode>();

        var baseTypes = new Stack<TypeReference>();
        var lastBaseType = TypeDef.BaseType;
        while (!UnitySerializationLogic.IsNonSerialized(lastBaseType)) {
            if (lastBaseType is GenericInstanceType genericInstanceType) {
                TypeResolver.Add(genericInstanceType);
            }

            baseTypes.Push(lastBaseType);
            lastBaseType = lastBaseType.Resolve().BaseType;
        }

        while (baseTypes.Count > 0) {
            var typeReference = baseTypes.Pop();
            var typeDefinition = typeReference.Resolve();
            foreach (var fieldDefinition in typeDefinition.Fields.Where(WillUnitySerialize)) {
                if (!IsHiddenByParentClass(baseTypes, fieldDefinition, TypeDef)) {
                    nodes.AddRange(ProcessingFieldRef(ResolveGenericFieldReference(fieldDefinition)));
                }
            }

            if (typeReference is GenericInstanceType genericInstanceType) {
                TypeResolver.Remove(genericInstanceType);
            }
        }

        foreach (var field in FilteredFields()) {
            nodes.AddRange(ProcessingFieldRef(field));
        }

        return nodes;
    }

    private bool WillUnitySerialize(FieldDefinition fieldDefinition) {
        var resolvedFieldType = TypeResolver.Resolve(fieldDefinition.FieldType);
        if (UnitySerializationLogic.ShouldNotTryToResolve(resolvedFieldType)) {
            return false;
        }

        if (!UnityEngineTypePredicates.IsUnityEngineObject(resolvedFieldType)) {
            if (resolvedFieldType.FullName == fieldDefinition.DeclaringType.FullName) {
                return false;
            }
        }

        return UnitySerializationLogic.WillUnitySerialize(fieldDefinition, TypeResolver);
    }

    private static bool IsHiddenByParentClass(IEnumerable<TypeReference> parentTypes, MemberReference fieldDefinition, TypeDefinition processingType) {
        return processingType.Fields.Any(f => f.Name == fieldDefinition.Name) || parentTypes.Any(t => t.Resolve().Fields.Any(f => f.Name == fieldDefinition.Name));
    }

    private IEnumerable<FieldDefinition> FilteredFields() {
        return TypeDef.Fields.Where(WillUnitySerialize)
            .Where(f =>
                UnitySerializationLogic.IsSupportedCollection(f.FieldType) ||
                !f.FieldType.IsGenericInstance ||
                UnitySerializationLogic.ShouldImplementIDeserializable(f.FieldType.Resolve()));
    }

    private FieldReference ResolveGenericFieldReference(FieldReference fieldRef) {
        var field = new FieldReference(fieldRef.Name, fieldRef.FieldType, ResolveDeclaringType(fieldRef.DeclaringType));
        return TypeDef.Module.ImportReference(field);
    }

    private TypeReference? ResolveDeclaringType(TypeReference declaringType) {
        var typeDefinition = declaringType.Resolve();
        if (typeDefinition is not { HasGenericParameters: true }) {
            return typeDefinition;
        }

        var genericInstanceType = new GenericInstanceType(typeDefinition);
        foreach (var genericParameter in typeDefinition.GenericParameters) {
            genericInstanceType.GenericArguments.Add(genericParameter);
        }

        return TypeResolver.Resolve(genericInstanceType);
    }

    private IEnumerable<ObjectNode> ProcessingFieldRef(FieldReference fieldDef) {
        var typeRef = TypeResolver.Resolve(fieldDef.FieldType);
        return TypeRefToObjectNodes(typeRef, fieldDef.Name, false);
    }

    private static bool IsStruct(TypeReference typeRef) => typeRef.IsValueType && !IsEnum(typeRef) && !typeRef.IsPrimitive;

    private static bool IsEnum(TypeReference typeRef) => !typeRef.IsArray && typeRef.Resolve().IsEnum;

    private static bool RequiresAlignment(TypeReference typeRef) {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (typeRef.MetadataType) {
            case MetadataType.Boolean:
            case MetadataType.Char:
            case MetadataType.SByte:
            case MetadataType.Byte:
            case MetadataType.Int16:
            case MetadataType.UInt16:
                return true;
            default:
                return UnitySerializationLogic.IsSupportedCollection(typeRef) && RequiresAlignment(CecilUtils.ElementTypeOfCollection(typeRef));
        }
    }

    private static bool IsSystemString(MemberReference typeRef) => typeRef.FullName == "System.String";

    private IEnumerable<ObjectNode> TypeRefToObjectNodes(TypeReference typeRef, string name, bool isElement) {
        var align = false;

        if (!IsStruct(TypeDef) || !UnityEngineTypePredicates.IsUnityEngineValueType(TypeDef)) {
            if (IsStruct(typeRef) || RequiresAlignment(typeRef)) {
                align = true;
            }
        }

        var nodes = new List<ObjectNode>();
        if (typeRef.IsPrimitive) {
            var primitiveName = typeRef.Name;
            primitiveName = primitiveName switch {
                "Boolean" => "bool",
                "Byte" => "UInt8",
                "SByte" => "SInt8",
                "Int16" => "SInt16",
                "UInt16" => "UInt16",
                "Int32" => "SInt32",
                "UInt32" => "UInt32",
                "Int64" => "SInt64",
                "UInt64" => "UInt64",
                "Char" => "char",
                "Double" => "double",
                "Single" => "float",
                _ => throw new NotSupportedException(),
            };

            if (isElement) {
                align = false;
            }

            nodes.Add(new ObjectNode(name, primitiveName, -1, align, primitiveName == "bool"));
        } else if (IsSystemString(typeRef)) {
            nodes.Add(new ObjectNode(name, "string", -1, false, false));
        } else if (IsEnum(typeRef)) {
            nodes.Add(new ObjectNode(name, "SInt32", 4, align, false));
        } else if (CecilUtils.IsGenericList(typeRef) || typeRef.IsArray) {
            var elementRef = typeRef.IsArray ? typeRef.GetElementType() : CecilUtils.ElementTypeOfCollection(typeRef);
            var array = new ObjectNode("Array", "Array", -1, false, false) {
                Properties = {
                    new ObjectNode("int", "size", 4, false, false),
                },
            };
            array.Properties.AddRange(TypeRefToObjectNodes(elementRef, "data", true));
            nodes.Add(new ObjectNode(name, typeRef.Name, -1, align, false) {
                Properties = new List<ObjectNode> {
                    array,
                },
            });
        } else if (UnityEngineTypePredicates.IsUnityEngineObject(typeRef)) {
            nodes.Add(new ObjectNode(name, $"PPtr<{typeRef.Name}>", 12, false, false));
        } else if (UnityEngineTypePredicates.IsSerializableUnityClass(typeRef) || UnityEngineTypePredicates.IsSerializableUnityStruct(typeRef)) {
            nodes.Add(new ObjectNode(name, typeRef.Name, -1, false, false));
        } else {
            nodes.Add(new ObjectNode(name, typeRef.Name, -1, align, false));
            var typeDef = typeRef.Resolve();
            var typeDefinitionConverter = new TypeDefinitionConverter(typeDef);
            nodes.AddRange(typeDefinitionConverter.ConvertToObjectNodes());
        }

        return nodes;
    }
}
