using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DragonLib;
using Snuggle.Core.Extensions;
using JetBrains.Annotations;
using Mono.Cecil;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core {
    [PublicAPI]
    public static class ObjectFactory {
        static ObjectFactory() {
            LoadImplementationTypes(Assembly.GetExecutingAssembly());
        }

        public static Type BaseType { get; } = typeof(SerializedObject);
        public static Type NamedBaseType { get; } = typeof(NamedObject);
        public static Type BaseClassIdType { get; } = typeof(UnityClassId);

        public static Dictionary<UnityGame, Dictionary<object, Type>> Implementations { get; set; } = new() {
            { UnityGame.Default, new Dictionary<object, Type>() },
        };

        public static Dictionary<UnityGame, Type> ClassIdExtensions { get; set; } = new();

        private static HashSet<object> ParserNotFoundWarningSet { get; } = new();

        public static Type GetClassIdForGame(UnityGame game) => ClassIdExtensions.TryGetValue(game, out var t) ? t : BaseClassIdType;

        public static object GetClassIdForGame(UnityGame game, int classId) {
            if (!ClassIdExtensions.TryGetValue(game, out var t)) {
                return (UnityClassId) classId;
            }

            var value = Enum.ToObject(t, classId);
            if (!Enum.IsDefined(t, value)) {
                return (UnityClassId) classId;
            }

            return value;
        }

        public static object GetClassIdForGame(UnityGame game, uint classId) => GetClassIdForGame(game, (int) classId);

        public static object GetClassIdForGame(UnityGame game, UnityClassId classId) => GetClassIdForGame(game, (int) classId);

        public static void LoadImplementationTypes(Assembly assembly) {
            foreach (var (type, attribute) in assembly.GetExportedTypes()
                .Where(x => x.IsAssignableTo(BaseType))
                .Select(x => (Type: x, Attribute: x.GetCustomAttribute<ObjectImplementationAttribute>()))
                .Where(x => x.Attribute != null)) {
                if (!Implementations.TryGetValue(attribute!.Game, out var gameImplementations)) {
                    gameImplementations = new Dictionary<object, Type>();
                    Implementations[attribute.Game] = gameImplementations;
                }

                gameImplementations[attribute.UnderlyingClassId] = type;
            }

            foreach (var (type, attribute) in assembly.GetExportedTypes()
                .Where(x => x.IsEnum)
                .Select(x => (Type: x, Attribute: x.GetCustomAttribute<ClassIdExtensionAttribute>()))
                .Where(x => x.Attribute != null)) {
                ClassIdExtensions[attribute!.Game] = type;
            }
        }

        public static SerializedObject GetInstance(Stream stream, UnityObjectInfo info, SerializedFile serializedFile, object? overrideType = null, UnityGame? overrideGame = null) {
            while (true) {
                if (FindObjectType(info, serializedFile, overrideType, ref overrideGame, out var hasImplementation, out var type)) {
                    continue;
                }

                if (type == null) {
                    throw new TypeImplementationNotFoundException(overrideType ?? info.ClassId);
                }

                using var reader = new BiEndianBinaryReader(stream, serializedFile.Header.IsBigEndian);
                try {
                    return CreateObjectInstance(reader, info, serializedFile, type, hasImplementation, overrideType, overrideGame);
                } catch (Exception e) {
                    serializedFile.Options.Logger.Error("Object", $"Failed to deserialize object {info.PathId} ({overrideType ?? info.ClassId:G}, {overrideGame ?? UnityGame.Default:G})", e);
                    if (overrideType == null ||
                        overrideType.Equals(UnityClassId.Object) == false && overrideType.Equals(UnityClassId.NamedObject) == false) {
                        overrideType = type.IsAssignableFrom(NamedBaseType) ? UnityClassId.NamedObject : UnityClassId.Object;
                        continue;
                    }

                    throw;
                }
            }
        }

        private static bool FindObjectType(
            UnityObjectInfo info,
            SerializedFile serializedFile,
            object? overrideType,
            ref UnityGame? overrideGame,
            out bool hasImplementation,
            out Type? type) {
            type = null;
            if (!Implementations.TryGetValue(overrideGame ?? serializedFile.Options.Game, out var gameImplementations)) {
                overrideGame = UnityGame.Default;
                gameImplementations = Implementations[UnityGame.Default];
            }

            hasImplementation = gameImplementations.TryGetValue(overrideType ?? info.ClassId, out type);
            if (!hasImplementation) {
                if (overrideGame != UnityGame.Default) {
                    overrideGame = UnityGame.Default;
                    return true;
                }

                type = BaseType;
                if (ParserNotFoundWarningSet.Add(overrideType ?? info.ClassId)) {
                    serializedFile.Options.Logger.Warning($"Can't find parser for type {overrideType ?? info.ClassId:G}");
                }
            }

            return false;
        }

        private static SerializedObject CreateObjectInstance(
            BiEndianBinaryReader reader,
            UnityObjectInfo info,
            SerializedFile serializedFile,
            Type type,
            bool hasImplementation,
            object? overrideType,
            UnityGame? overrideGame) {
#if DEBUG
            var currentMemory = GC.GetTotalMemory(false);
#endif
            var instance = Activator.CreateInstance(type, reader, info, serializedFile);
#if DEBUG
            var memoryUse = GC.GetTotalMemory(false) - currentMemory;
#endif
            if (instance is not SerializedObject serializedObject) {
                throw new InvalidTypeImplementation(overrideType ?? info.ClassId);
            }

#if DEBUG
            if (memoryUse >= 1.ToMebiByte()) {
                serializedFile.Options.Logger.Warning("Object",
                    $"Using {memoryUse.GetHumanReadableBytes()} of memory to load object {info.PathId} ({overrideType ?? info.ClassId:G}, {overrideGame ?? UnityGame.Default:G}), consider moving some things to ToSerialize()");
            }
#endif

            if (overrideType == null &&
                hasImplementation &&
                reader.Unconsumed > 0 &&
                !serializedObject.ShouldDeserialize) {
                serializedFile.Options.Logger.Warning("Object",
                    $"{reader.Unconsumed} bytes left unconsumed in buffer and object {info.PathId} ({overrideType ?? info.ClassId:G}, {overrideGame ?? UnityGame.Default:G}) is not marked for deserialization! Check implementation");
            }

            if (serializedObject is ISerializedResource resource) {
                serializedObject.Size += resource.StreamData.Size;
            }

            return serializedObject;
        }

        public static object? CreateObject(BiEndianBinaryReader reader, ObjectNode node, SerializedFile file, string? skipUntil = null) {
            var start = reader.BaseStream.Position;
            object? value = null;
            try {
                if (node.TypeName.StartsWith("PPtr<") ||
                    node.TypeName == "PPtr`1") {
                    value = PPtr<SerializedObject>.FromReader(reader, file);
                } else {
                    switch (node.TypeName.ToLowerInvariant()) {
                        case "char":
                        case "byte":
                        case "bool":
                        case "boolean":
                        case "uint8": {
                            var tmp = reader.ReadByte();
                            value = node.IsBoolean ? tmp == 1 : tmp;
                            break;
                        }
                        case "unsigned short":
                        case "ushort":
                        case "uint16": {
                            var tmp = reader.ReadUInt16();
                            value = node.IsBoolean ? tmp == 1 : tmp;
                            break;
                        }
                        case "unsigned int":
                        case "uint":
                        case "uint32": {
                            var tmp = reader.ReadUInt32();
                            value = node.IsBoolean ? tmp == 1 : tmp;
                            break;
                        }
                        case "unsigned long long":
                        case "ulonglong":
                        case "uint64": {
                            var tmp = reader.ReadUInt64();
                            value = node.IsBoolean ? tmp == 1 : tmp;
                            break;
                        }
                        case "short":
                        case "sint16":
                        case "int16": {
                            var tmp = reader.ReadInt16();
                            value = node.IsBoolean ? tmp == 1 : tmp;
                            break;
                        }
                        case "type*":
                        case "int":
                        case "sint32":
                        case "int32": {
                            var tmp = reader.ReadInt32();
                            value = node.IsBoolean ? tmp == 1 : tmp;
                            break;
                        }
                        case "filesize":
                        case "long long":
                        case "longlong":
                        case "sint64":
                        case "int64": {
                            var tmp = reader.ReadInt64();
                            value = node.IsBoolean ? tmp == 1 : tmp;
                            break;
                        }
                        case "float32":
                        case "float":
                        case "single": {
                            value = reader.ReadSingle();
                            break;
                        }
                        case "float64":
                        case "double": {
                            value = reader.ReadDouble();
                            break;
                        }
                        case "float128":
                        case "decimal":
                        case "double double": {
                            value = reader.ReadDecimal();
                            break;
                        }
                        case "string": {
                            value = reader.ReadString32();
                            break;
                        }
                        case "hash128": {
                            value = reader.ReadBytes(16);
                            break;
                        }
                        case "typelessdata": {
                            var size = reader.ReadInt32();
                            value = reader.ReadBytes(size);
                            break;
                        }
                        case "guid": {
                            value = new Guid(reader.ReadBytes(16));
                            break;
                        }
                        case "array": {
                            var dataNode = node.Properties[1];
                            var count = reader.ReadInt32();
                            var list = new List<object?>();
                            list.EnsureCapacity(count);
                            for (var i = 0; i < count; ++i) {
                                var subValue = CreateObject(reader, dataNode, file);
                                list.Add(subValue);
                                if (subValue == null) {
                                    break;
                                }
                            }

                            value = list;
                            break;
                        }
                        case "vector": {
                            if (node.Properties[0].TypeName != "Array") {
                                throw new NotSupportedException("Vector is not an Array type");
                            }

                            return CreateObject(reader, node.Properties[0], file);
                        }
                        case "map": {
                            if (node.Properties[0].TypeName != "Array") {
                                throw new NotSupportedException("Map is not an Array type");
                            }

                            if (node.Properties[0].Properties[1].TypeName != "pair") {
                                throw new NotSupportedException("Map.Array is not a pair type");
                            }

                            var dataNode = node.Properties[0].Properties[1];
                            var keyNode = dataNode.Properties[0];
                            var valueNode = dataNode.Properties[1];
                            var count = reader.ReadInt32();
                            var dict = new Dictionary<object, object?>();
                            dict.EnsureCapacity(count);
                            for (var i = 0; i < count; ++i) {
                                var keyValue = CreateObject(reader, keyNode, file);
                                if (keyValue == null) {
                                    break;
                                }

                                var valueValue = CreateObject(reader, valueNode, file);
                                dict[keyValue] = valueValue;
                                if (valueValue == null) {
                                    break;
                                }
                            }

                            value = dict;
                            break;
                        }
                        default: {
                            var dict = new Dictionary<string, object?>();
                            IEnumerable<ObjectNode> properties = node.Properties;
                            if (!string.IsNullOrEmpty(skipUntil)) {
                                properties = properties.SkipWhile(x => x.Name != skipUntil).Skip(1);
                            }

                            foreach (var property in properties) {
                                var subValue = CreateObject(reader, property, file);
                                dict[property.Name] = subValue;
                                if (subValue == null) {
                                    break;
                                }
                            }

                            value = dict;
                            break;
                        }
                    }
                }
            } catch {
                // ignored! Log this!
            }

            if (node.Size > 0) {
                reader.BaseStream.Seek(start + node.Size, SeekOrigin.Begin);
            }

            if (node.IsAligned) {
                reader.Align();
            }

            return value;
        }

        public static ObjectNode? FindObjectNode(string name, UnityTypeTree? typeTree, AssetCollection collection) {
            if (collection.Types.TryGetValue(name, out var cachedType)) {
                return cachedType;
            }

            if (typeTree != null) {
                var type = ObjectNode.FromUnityTypeTree(typeTree);
                if (type.Properties.Count > 0 &&
                    !string.IsNullOrEmpty(type.TypeName)) {
                    collection.Types[name] = type;
                    return type;
                }
            }

            return null;
        }

        public static ObjectNode? FindObjectNode(string name, MonoScript script, AssetCollection collection, RequestAssemblyPath? callback) {
            try {
                var assemblyName = script.AssemblyName;
                if (assemblyName.EndsWith(".dll")) {
                    assemblyName = assemblyName[..^4];
                }

                if (!collection.Assemblies.HasAssembly(assemblyName)) {
                    if (callback == null) {
                        return null;
                    }

                    var (path, options) = callback.Invoke(assemblyName);
                    collection.LoadFile(path, options);

                    if (!collection.Assemblies.HasAssembly(assemblyName)) {
                        return null;
                    }
                }

                var objectNode = new ObjectNode("Base", "MonoBehavior", -1, false, false) {
                    Properties = new List<ObjectNode> { // these all get skipped.
                        new("m_GameObject", "PPtr`1", 12, false, false),
                        new("m_Enabled", "UInt8", 1, true, true),
                        new("m_Script", "PPtr`1", 12, false, false),
                        new("m_Name", "string", -1, false, false),
                    },
                };

                if (collection.Assemblies.HasAssembly(assemblyName)) {
                    objectNode.Properties.AddRange(GetObjectType(collection.Assemblies.Resolve(assemblyName), script));
                    return objectNode;
                }
            } catch {
                // ignored
            }

            // todo: global metadata

            return null;
        }

        private static List<ObjectNode> GetObjectType(AssemblyDefinition assembly, MonoScript script) => throw new NotImplementedException();

        // There's a lot of fucky stuff with how Unity determines fields.

        private static List<ObjectNode> GetObjectType(TypeReference? reference, Dictionary<string, List<ObjectNode>> cache) {
            if (reference == null) {
                return new List<ObjectNode>();
            }

            if (!cache.TryGetValue(reference.FullName, out var cached)) {
                throw new NotImplementedException();
            }

            return cached;
        }

        private static ObjectNode CreateObjectNode(string name, TypeReference? typeReference, Dictionary<string, List<ObjectNode>> cache) {
            if (typeReference == null) {
                throw new NotSupportedException("Type reference is null");
            }

            var typeDefinition = typeReference.Resolve();
            if (typeReference.FullName.StartsWith("System.Collections.Generic.List`1") ||
                typeReference.IsArray) {
                var arrayType = typeReference.IsArray ? ((ArrayType) typeReference).ElementType : ((GenericInstanceType) typeReference).GenericArguments[0];
                return new ObjectNode(name, "vector", -1, false, false) {
                    Properties = new List<ObjectNode> {
                        new("Array", "Array", -1, true, false) {
                            Properties = new List<ObjectNode> {
                                new("count", "int", 4, false, false),
                                CreateObjectNode("data", arrayType, cache),
                            },
                        },
                    },
                };
            }

            if (typeReference.FullName.StartsWith("System.Collections.Generic.Dictionary`2")) {
                var generics = ((GenericInstanceType) typeReference).GenericArguments;
                var keyType = generics[0].Resolve();
                var valueType = generics[1].Resolve();
                return new ObjectNode(name, "map", -1, false, false) {
                    Properties = new List<ObjectNode> {
                        new("Array", "Array", -1, true, false) {
                            Properties = new List<ObjectNode> {
                                new("count", "int", 4, false, false),
                                new("data", "pair", -1, false, false) {
                                    Properties = new List<ObjectNode> {
                                        CreateObjectNode("first", keyType, cache),
                                        CreateObjectNode("second", valueType, cache),
                                    },
                                },
                            },
                        },
                    },
                };
            }

            if (typeReference.Namespace == "System") {
                switch (typeReference.Name) {
                    case "String":
                        return new ObjectNode(name, "string", -1, false, false);
                    case "Boolean":
                    case "UInt8":
                    case "Char":
                        return new ObjectNode(name, "byte", 1, true, typeReference.Name[0] == 'B');
                    case "Int8":
                        return new ObjectNode(name, "sbyte", 1, true, false);
                    case "UInt16":
                        return new ObjectNode(name, "unsigned short", 2, true, false);
                    case "Int16":
                        return new ObjectNode(name, "short", 2, true, false);
                    case "UInt32":
                        return new ObjectNode(name, "unsigned int", 4, false, false);
                    case "Int32":
                        return new ObjectNode(name, "int", 4, false, false);
                    case "UInt64":
                        return new ObjectNode(name, "unsigned long long", 8, false, false);
                    case "Int64":
                        return new ObjectNode(name, "long long", 8, false, false);
                    case "Single":
                        return new ObjectNode(name, "float", 4, false, false);
                    case "Double":
                        return new ObjectNode(name, "double", 8, false, false);
                    case "Guid":
                        return new ObjectNode(name, "Guid", 8, false, false);
                }
            }

            if (typeDefinition.IsEnum) {
                return new ObjectNode(name, "int", 4, false, false);
            }

            if (typeReference.FullName == "UnityEngine.Hash128") {
                return new ObjectNode(name, "Hash128", 16, false, false);
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (typeDefinition.IsAssignableTo("UnityEngine.Object")) {
                return new ObjectNode(name, "PPtr`1", 12, false, false);
            }

            return new ObjectNode(name, typeReference.Name, typeDefinition.ClassSize, false, false) { Properties = GetObjectType(typeDefinition, cache) };
        }
    }
}
