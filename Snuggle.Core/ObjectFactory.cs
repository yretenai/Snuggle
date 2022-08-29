using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Serilog;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Animation;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core;

public static class ObjectFactory {
    static ObjectFactory() {
        LoadImplementationTypes(Assembly.GetExecutingAssembly());
    }

    public static Type BaseType { get; } = typeof(SerializedObject);
    public static Type NamedType { get; } = typeof(NamedObject);
    public static Type ScriptedType { get; } = typeof(MonoBehaviour);
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
            if (!TryFindObjectType(info, serializedFile, overrideType, ref overrideGame, out var type)) {
                continue;
            }

            if (type == null) {
                throw new TypeImplementationNotFound(overrideType ?? info.ClassId);
            }

            using var reader = new BiEndianBinaryReader(stream, serializedFile.Header.IsBigEndian);
            try {
                return CreateObjectInstance(reader, info, serializedFile, type, overrideType);
            } catch (Exception e) {
                Log.Error(e, "Failed to deserialize object {info.PathId} ({ClassId:G}, {Game:G})", info.PathId, overrideType ?? info.ClassId, overrideGame ?? UnityGame.Default);
                if (overrideType == null || overrideType.Equals(UnityClassId.Object) == false && overrideType.Equals(UnityClassId.NamedObject) == false) {
                    overrideType = Utils.ClassIdIsNamedObject(info.ClassId) ? UnityClassId.NamedObject : UnityClassId.Object;
                    continue;
                }

                throw;
            }
        }
    }

    private static bool TryFindObjectType(UnityObjectInfo info, SerializedFile serializedFile, object? overrideType, ref UnityGame? overrideGame, [MaybeNullWhen(false)] out Type type) {
        type = null;
        overrideGame ??= serializedFile.Options.Game;

        if (!Implementations.TryGetValue(overrideGame.Value, out var gameImplementations)) {
            overrideGame = UnityGame.Default;
            gameImplementations = Implementations[UnityGame.Default];
        }

        if (!gameImplementations.TryGetValue(overrideType ?? info.ClassId, out type)) {
            if (overrideGame is not UnityGame.Default) {
                overrideGame = UnityGame.Default;
                return false;
            }

            if (ParserNotFoundWarningSet.Add(overrideType ?? info.ClassId)) {
                Log.Warning("Can't find parser for type {ClassId:G}", overrideType ?? info.ClassId);
            }
        }

        type ??= BaseType;

        if (overrideGame is UnityGame.Default && serializedFile.Options.Game is not UnityGame.Default && DisabledDefaultImplementations.TryGetValue(overrideType ?? info.ClassId, out var disabledGames) && disabledGames.Contains(serializedFile.Options.Game)) {
            type = BaseType;
        }

        if (type == BaseType && Utils.ClassIdIsNamedObject(overrideType ?? info.ClassId)) {
            type = NamedType;
        } 

        return true;
    }

    private static SerializedObject CreateObjectInstance(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile, Type type, object? overrideType) {
        var instance = Activator.CreateInstance(type, reader, info, serializedFile);
        if (instance is not SerializedObject serializedObject) {
            throw new InvalidTypeImplementation(overrideType ?? info.ClassId);
        }

        if (serializedObject is ISerializedResource resource) {
            serializedObject.Size += resource.StreamData.Size;
        }

        return serializedObject;
    }

    public static object? CreateObject(BiEndianBinaryReader reader, ObjectNode node, SerializedFile file, string? skipUntil = null) {
        if (reader.Unconsumed == 0) {
            return null;
        }

        var start = reader.BaseStream.Position;
        object? value;
        try {
            if (node.TypeName.StartsWith("PPtr<") || node.TypeName == "PPtr`1") {
                value = PPtr<SerializedObject>.FromReader(reader, file);
            } else {
                switch (node.TypeName.ToLowerInvariant()) {
                    case "char":
                    case "byte":
                    case "bool":
                    case "boolean":
                    case "uint8": {
                        var tmp = reader.ReadByte();
                        value = node.Meta.HasFlag(UnityTransferMetaFlags.TreatIntegerValueAsBoolean) ? tmp == 1 : tmp;
                        break;
                    }
                    case "unsigned short":
                    case "ushort":
                    case "uint16": {
                        var tmp = reader.ReadUInt16();
                        value = node.Meta.HasFlag(UnityTransferMetaFlags.TreatIntegerValueAsBoolean) ? tmp == 1 : tmp;
                        break;
                    }
                    case "unsigned int":
                    case "uint":
                    case "uint32": {
                        var tmp = reader.ReadUInt32();
                        value = node.Meta.HasFlag(UnityTransferMetaFlags.TreatIntegerValueAsBoolean) ? tmp == 1 : tmp;
                        break;
                    }
                    case "unsigned long long":
                    case "ulonglong":
                    case "uint64": {
                        var tmp = reader.ReadUInt64();
                        value = node.Meta.HasFlag(UnityTransferMetaFlags.TreatIntegerValueAsBoolean) ? tmp == 1 : tmp;
                        break;
                    }
                    case "short":
                    case "sint16":
                    case "int16": {
                        var tmp = reader.ReadInt16();
                        value = node.Meta.HasFlag(UnityTransferMetaFlags.TreatIntegerValueAsBoolean) ? tmp == 1 : tmp;
                        break;
                    }
                    case "type*":
                    case "int":
                    case "sint32":
                    case "int32": {
                        var tmp = reader.ReadInt32();
                        if (node.EnumValues != null && node.EnumValues.TryGetValue(tmp, out var enumValue)) {
                            value = enumValue;
                        } else {
                            value = node.Meta.HasFlag(UnityTransferMetaFlags.TreatIntegerValueAsBoolean) ? tmp == 1 : tmp;
                        }

                        break;
                    }
                    case "filesize":
                    case "long long":
                    case "longlong":
                    case "sint64":
                    case "int64": {
                        var tmp = reader.ReadInt64();
                        value = node.Meta.HasFlag(UnityTransferMetaFlags.TreatIntegerValueAsBoolean) ? tmp == 1 : tmp;
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
                        value = AnimationCurve<float>.FromReader(reader, file);
                        break;
                    }
                    case "gradient": {
                        value = Gradient.FromReader(reader, file);
                        break;
                    }
                    case "guistyle": {
                        value = GUIStyle.FromReader(reader, file);
                        break;
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
                        value = reader.ReadStruct<SphericalHarmonicsL2>();
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
                        var list = new object?[count];
                        for (var i = 0; i < count; ++i) {
                            var subValue = CreateObject(reader, dataNode, file);
                            list[i] = subValue;
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
                        if (dict.Count == 1 && dict.TryGetValue("Array", out var arrayValue)) {
                            value = arrayValue;
                        }

                        break;
                    }
                }
            }
        } catch (Exception e) {
            Log.Error(e, "Failed deserializing field {Node}", node);
            throw;
        }

        if (node.Size > 0) {
            reader.BaseStream.Seek(start + node.Size, SeekOrigin.Begin);
        }

        if (node.Meta.HasFlag(UnityTransferMetaFlags.AlignBytes)) {
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

                var (path, options) = callback.Invoke(script.AssemblyName);
                if (string.IsNullOrEmpty(path)) {
                    return null;
                }

                if (options == null) {
                    return null;
                }

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
        } catch (Exception e) {
            Log.Error(e, "Failed to convert Cecil type");
        }

        return null;
    }
}
