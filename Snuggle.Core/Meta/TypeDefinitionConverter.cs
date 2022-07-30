using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Snuggle.Core.Models.Serialization;
using Unity.CecilTools;
using Unity.CecilTools.Extensions;
using Unity.SerializationLogic;

namespace Snuggle.Core.Meta;

public class TypeDefinitionConverter {
    private readonly TypeDefinition TypeDef;
    private readonly TypeReference TypeRef;
    private readonly TypeResolver TypeResolver;

    public TypeDefinitionConverter(TypeDefinition typeDef, TypeReference typeRef) {
        TypeDef = typeDef;
        TypeRef = typeRef;
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
        var resolvedFieldType = default(TypeReference);
        if (TypeRef is GenericInstanceType genericInstanceType) {
            if (fieldDefinition.FieldType.IsGenericParameter) {
                var genericIndex = TypeDef.GenericParameters.Select((x, i) => (x, i)).FirstOrDefault(x => x.x.Name == fieldDefinition.FieldType.Name).i;
                resolvedFieldType = genericInstanceType.GenericArguments.ElementAtOrDefault(genericIndex);
                fieldDefinition.FieldType = resolvedFieldType;
            }

            if (fieldDefinition.FieldType is GenericInstanceType { ContainsGenericParameter: true } fieldGenericInstanceType) {
                var genericTypes = TypeDef.GenericParameters.Select((x, i) => (x.Name, i)).ToDictionary(x => x.Item1, x => x.i);
                var generics = new TypeReference[fieldGenericInstanceType.GenericArguments.Count];
                for (var index = 0; index < fieldGenericInstanceType.GenericArguments.Count; index++) {
                    var genericArgument = fieldGenericInstanceType.GenericArguments[index];
                    if (genericArgument.DeclaringType.Resolve() == TypeDef && genericTypes.TryGetValue(genericArgument.Name, out var genericIndex)) {
                        generics[index] = genericInstanceType.GenericArguments[genericIndex];
                    } else {
                        generics[index] = genericArgument;
                    }
                }

                var baseType = fieldGenericInstanceType.GetElementType();
                var fixedType = baseType.MakeGenericInstanceType(generics);
                resolvedFieldType = TypeResolver.Resolve(fixedType);
                fieldDefinition.FieldType = resolvedFieldType;
            }
        }

        resolvedFieldType ??= TypeResolver.Resolve(fieldDefinition.FieldType);

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
        var flags = UnityTransferMetaFlags.None;

        var typeDef = typeRef.Resolve();
        if (!IsStruct(TypeDef) || !UnityEngineTypePredicates.IsUnityEngineValueType(TypeDef)) {
            if (IsStruct(typeDef) || RequiresAlignment(typeRef)) {
                flags |= UnityTransferMetaFlags.AlignBytes;
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

            flags |= primitiveName == "bool" ? UnityTransferMetaFlags.TreatIntegerValueAsBoolean : UnityTransferMetaFlags.None;

            if (isElement && flags.HasFlag(UnityTransferMetaFlags.AlignBytes)) {
                flags ^= UnityTransferMetaFlags.AlignBytes;
            }

            nodes.Add(new ObjectNode(name, primitiveName, -1, flags, UnityTransferTypeFlags.None, null));
        } else if (IsSystemString(typeRef)) {
            nodes.Add(new ObjectNode(name, "string", -1, UnityTransferMetaFlags.None, UnityTransferTypeFlags.None, null));
        } else if (IsEnum(typeDef)) {
            nodes.Add(new ObjectNode(name, "SInt32", 4, flags, UnityTransferTypeFlags.None, GetEnumNames(typeDef)));
        } else if (CecilUtils.IsGenericList(typeRef) || typeRef.IsArray) {
            var elementRef = typeRef.IsArray ? typeRef.GetElementType() : CecilUtils.ElementTypeOfCollection(typeRef);
            var array = new ObjectNode("Array", "Array", -1, UnityTransferMetaFlags.None, UnityTransferTypeFlags.None, null) {
                Properties = {
                    new ObjectNode("int", "size", 4, UnityTransferMetaFlags.None, UnityTransferTypeFlags.None, null),
                },
            };
            array.Properties.AddRange(TypeRefToObjectNodes(elementRef, "data", true));
            nodes.Add(new ObjectNode(name, $"Array<{typeRef.FullName}>", -1, flags, UnityTransferTypeFlags.Array, null) {
                Properties = new List<ObjectNode> {
                    array,
                },
            });
        } else if (UnityEngineTypePredicates.IsUnityEngineObject(typeRef)) {
            nodes.Add(new ObjectNode(name, $"PPtr<{typeRef.Name}>", 12, UnityTransferMetaFlags.None, UnityTransferTypeFlags.None, null));
        } else if (UnityEngineTypePredicates.IsSerializableUnityClass(typeRef) || UnityEngineTypePredicates.IsSerializableUnityStruct(typeRef)) {
            nodes.Add(new ObjectNode(name, typeRef.Name, -1, UnityTransferMetaFlags.None, UnityTransferTypeFlags.None, null));
        } else {
            var obj = new ObjectNode(name, $"${typeRef.FullName}", -1, flags, UnityTransferTypeFlags.None, null);
            var typeDefinitionConverter = new TypeDefinitionConverter(typeDef, typeRef);
            obj.Properties.AddRange(typeDefinitionConverter.ConvertToObjectNodes());
            nodes.Add(obj);
        }

        return nodes;
    }

    private static Dictionary<int, string>? GetEnumNames(TypeReference typeRef) {
        var type = typeRef.Resolve();
        if (!type.IsEnum()) {
            return null;
        }

        var enums = new Dictionary<int, string>();
        foreach (var field in type.Fields) {
            if (field.HasConstant) {
                try {
                    var enumValue = Convert.ToInt32(field.Constant);
                    if (!enums.ContainsKey(enumValue)) {
                        enums[enumValue] = field.Name;
                    }
                } catch {
                    return null;
                }
            }
        }

        return enums;
    }
}
