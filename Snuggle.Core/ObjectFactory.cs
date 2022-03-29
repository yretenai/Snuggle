using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using DragonLib;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Extensions;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core;

public static class ObjectFactory {
    static ObjectFactory() {
        LoadImplementationTypes(Assembly.GetExecutingAssembly());
    }

    public static Type BaseType { get; } = typeof(SerializedObject);
    public static Type NamedBaseType { get; } = typeof(NamedObject);
    public static Type BaseClassIdType { get; } = typeof(UnityClassId);

    public static Dictionary<UnityGame, Dictionary<object, Type>> Implementations { get; set; } = new() { { UnityGame.Default, new Dictionary<object, Type>() } };
    public static Dictionary<object, HashSet<UnityGame>> DisabledDefaultImplementations { get; set; } = new();

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
        foreach (var (type, attribute) in assembly.GetExportedTypes().Where(x => x.IsAssignableTo(BaseType)).Select(x => (Type: x, Attribute: x.GetCustomAttribute<ObjectImplementationAttribute>())).Where(x => x.Attribute != null)) {
            if (!Implementations.TryGetValue(attribute!.Game, out var gameImplementations)) {
                gameImplementations = new Dictionary<object, Type>();
                Implementations[attribute.Game] = gameImplementations;
            }

            gameImplementations[attribute.UnderlyingClassId] = type;

            if (attribute.DisabledGames.Length > 0) {
                if (!DisabledDefaultImplementations.TryGetValue(attribute.UnderlyingClassId, out var disabledImplemtations)) {
                    disabledImplemtations = new HashSet<UnityGame>();
                    DisabledDefaultImplementations[attribute.UnderlyingClassId] = disabledImplemtations;
                }

                disabledImplemtations.EnsureCapacity(disabledImplemtations.Count + attribute.DisabledGames.Length);
                foreach (var game in attribute.DisabledGames) {
                    disabledImplemtations.Add(game);
                }
            }
        }

        foreach (var (type, attribute) in assembly.GetExportedTypes().Where(x => x.IsEnum).Select(x => (Type: x, Attribute: x.GetCustomAttribute<ClassIdExtensionAttribute>())).Where(x => x.Attribute != null)) {
            ClassIdExtensions[attribute!.Game] = type;
        }
    }

    public static SerializedObject GetInstance(Stream stream, UnityObjectInfo info, SerializedFile serializedFile, object? overrideType = null, UnityGame? overrideGame = null) {
        while (true) {
            if (!TryFindObjectType(info, serializedFile, overrideType, ref overrideGame, out var hasImplementation, out var type)) {
                continue;
            }

            if (type == null) {
                throw new TypeImplementationNotFound(overrideType ?? info.ClassId);
            }

            using var reader = new BiEndianBinaryReader(stream, serializedFile.Header.IsBigEndian);
            try {
                return CreateObjectInstance(
                    reader,
                    info,
                    serializedFile,
                    type,
                    hasImplementation,
                    overrideType,
                    overrideGame);
            } catch (Exception e) {
                serializedFile.Options.Logger.Error("Object", $"Failed to deserialize object {info.PathId} ({overrideType ?? info.ClassId:G}, {overrideGame ?? UnityGame.Default:G})", e);
                if (overrideType == null || overrideType.Equals(UnityClassId.Object) == false && overrideType.Equals(UnityClassId.NamedObject) == false) {
                    overrideType = type.IsAssignableFrom(NamedBaseType) ? UnityClassId.NamedObject : UnityClassId.Object;
                    continue;
                }

                throw;
            }
        }
    }

    private static bool TryFindObjectType(UnityObjectInfo info, SerializedFile serializedFile, object? overrideType, ref UnityGame? overrideGame, out bool hasImplementation, [MaybeNullWhen(false)] out Type type) {
        type = null;
        overrideGame ??= serializedFile.Options.Game;

        if (!Implementations.TryGetValue(overrideGame.Value, out var gameImplementations)) {
            overrideGame = UnityGame.Default;
            gameImplementations = Implementations[UnityGame.Default];
        }

        hasImplementation = gameImplementations.TryGetValue(overrideType ?? info.ClassId, out type);
        if (!hasImplementation) {
            if (overrideGame is not UnityGame.Default) {
                overrideGame = UnityGame.Default;
                return false;
            }

            if (ParserNotFoundWarningSet.Add(overrideType ?? info.ClassId)) {
                serializedFile.Options.Logger.Warning("SerializedObject", $"Can't find parser for type {overrideType ?? info.ClassId:G}");
            }
        }

        type ??= BaseType;

        if (overrideGame is UnityGame.Default && serializedFile.Options.Game is not UnityGame.Default && DisabledDefaultImplementations.TryGetValue(overrideType ?? info.ClassId, out var disabledGames) && disabledGames.Contains(serializedFile.Options.Game)) {
            type = BaseType;
        }

        return true;
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
            serializedFile.Options.Logger.Warning("Object", $"Using {memoryUse.GetHumanReadableBytes()} of memory to load object {info.PathId} ({overrideType ?? info.ClassId:G}, {overrideGame ?? UnityGame.Default:G}), consider moving some things to ToSerialize()");
        }

        if (overrideType == null && hasImplementation && reader.Unconsumed > 0 && !serializedObject.ShouldDeserialize) {
            serializedFile.Options.Logger.Warning("Object", $"{reader.Unconsumed} bytes left unconsumed in buffer and object {info.PathId} ({overrideType ?? info.ClassId:G}, {overrideGame ?? UnityGame.Default:G}) is not marked for deserialization! Check implementation");
        }
#endif

        if (serializedObject is ISerializedResource resource) {
            serializedObject.Size += resource.StreamData.Size;
        }

        return serializedObject;
    }

    public static object? CreateObject(BiEndianBinaryReader reader, ObjectNode node, SerializedFile file, string? skipUntil = null) {
        var start = reader.BaseStream.Position;
        object? value = null;
        try {
            if (node.TypeName.StartsWith("PPtr<")) {
                var ptr = PPtr<SerializedObject>.FromReader(reader, file);
                ptr.Tag = node.TypeName.Split('<')[1][..^1];
                value = ptr;
            } else if (node.TypeName == "PPtr`1") {
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
                    case "animationcurve": {
                        // todo(naomi): value = reader.ReadStruct<AnimationCurve>();
                        throw new NotImplementedException();
                    }
                    case "gradient": {
                        // todo(naomi): value = reader.ReadStruct<Gradient>();
                        throw new NotImplementedException();
                    }
                    case "guistyle": {
                        // todo(naomi): value = reader.ReadStruct<GUIStyle>();
                        throw new NotImplementedException();
                    }
                    case "rectoffset": {
                        value = reader.ReadStruct<RectOffset>();
                        break;
                    }
                    case "color32": {
                        value = reader.ReadStruct<Color32>();
                        break;
                    }
                    case "matrix4x4": {
                        value = reader.ReadStruct<Matrix4X4>();
                        break;
                    }
                    case "sphericalharmonicsl2": {
                        value = reader.ReadArray<float>(3 * 9).ToArray(); // todo(naomi): SphericalHarmonicsL2 3x9 float
                        break;
                    }
                    case "propertyname": {
                        value = reader.ReadString32();
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
            if (type.Properties.Count > 0 && !string.IsNullOrEmpty(type.TypeName)) {
                collection.Types[name] = type;
                return type;
            }
        }

        return null;
    }

    public static ObjectNode? FindObjectNode(string name, MonoScript script, AssetCollection collection, RequestAssemblyPath? callback) {
        try {
            if (collection.Types.TryGetValue(name, out var cachedType)) {
                return cachedType;
            }

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

            if (collection.Assemblies.HasAssembly(assemblyName)) {
                var assembly = collection.Assemblies.Resolve(assemblyName).MainModule;
                var objectNode = ObjectNode.FromCecil(assembly.GetType(name));
                collection.Types[name] = objectNode;
                return objectNode;
            }
        } catch {
            // ignored
        }

        return null;
    }
}
