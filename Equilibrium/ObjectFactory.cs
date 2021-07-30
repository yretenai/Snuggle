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

        public static Type GetClassIdForGame(UnityGame game) {
            return ClassIdExtensions.TryGetValue(game, out var t) ? t : BaseClassIdType;
        }

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
    }
}
