using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Equilibrium.Implementations;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium {
    [PublicAPI]
    public static class ObjectFactory {
        static ObjectFactory() {
            LoadImplementationTypes(Assembly.GetExecutingAssembly());
        }

        public static Type BaseType { get; } = typeof(SerializedObject);
        public static Type BaseClassIdType { get; } = typeof(UnityClassId);

        public static Dictionary<UnityGame, Dictionary<object, Type>> Implementations { get; set; } = new() {
            { UnityGame.Default, new Dictionary<object, Type>() },
        };

        public static Dictionary<UnityGame, Type> ClassIdExtensions { get; set; } = new();

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
                if (!Implementations.TryGetValue(overrideGame ?? serializedFile.Options.Game, out var gameImplementations)) {
                    overrideGame = UnityGame.Default;
                    gameImplementations = Implementations[UnityGame.Default];
                }

                var hasImplementation = gameImplementations.TryGetValue(overrideType ?? info.ClassId, out var type);
                if (!hasImplementation) {
                    if (overrideGame != UnityGame.Default) {
                        overrideGame = UnityGame.Default;
                        continue;
                    }

                    type = BaseType;
                }

                if (type == null) {
                    throw new NullReferenceException();
                }

                using var reader = new BiEndianBinaryReader(serializedFile.OpenFile(info, stream), serializedFile.Header.IsBigEndian, true);
                var instance = Activator.CreateInstance(type, reader, info, serializedFile);
                if (instance is not SerializedObject serializedObject) {
                    throw new InvalidOperationException();
                }

                if (hasImplementation &&
                    reader.Unconsumed > 0 &&
                    !serializedObject.ShouldDeserialize) {
                    var msg = $"{reader.Unconsumed} bytes left unconsumed in buffer and {serializedObject.ClassId:G} ({serializedObject.PathId}) object is not marked for deserialization! Check implementation.";
                    Debug.WriteLine(msg);
                    serializedFile.Options.Reporter?.Log(msg);
                }

                return serializedObject;
            }
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
                                throw new NotSupportedException();
                            }

                            return CreateObject(reader, node.Properties[0], file);
                        }
                        case "map": {
                            if (node.Properties[0].TypeName != "Array") {
                                throw new NotSupportedException();
                            }

                            if (node.Properties[0].Properties[1].TypeName != "pair") {
                                throw new NotSupportedException();
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
    }
}
