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

        public static Dictionary<UnityGame, Dictionary<ClassId, Type>> Implementations { get; set; } = new() {
            { UnityGame.Default, new Dictionary<ClassId, Type>() },
        };

        public static void LoadImplementationTypes(Assembly assembly) {
            foreach (var (type, attribute) in assembly.GetExportedTypes()
                .Where(x => x.IsAssignableTo(BaseType))
                .Select(x => (Type: x, Attribute: x.GetCustomAttribute<ObjectImplementationAttribute>()))
                .Where(x => x.Attribute != null)) {
                if (!Implementations.TryGetValue(attribute!.Game, out var gameImplementations)) {
                    gameImplementations = new Dictionary<ClassId, Type>();
                    Implementations[attribute.Game] = gameImplementations;
                }

                gameImplementations[attribute.ClassId] = type;
            }
        }

        public static SerializedObject GetInstance(Stream stream, UnityObjectInfo info, SerializedFile serializedFile, ClassId? overrideType = null, UnityGame? overrideGame = null) {
            while (true) {
                if (!Implementations.TryGetValue(overrideGame ?? serializedFile.Options.Game, out var gameImplementations)) {
                    overrideGame = UnityGame.Default;
                    gameImplementations = Implementations[UnityGame.Default];
                }

                if (!gameImplementations.TryGetValue(overrideType ?? info.ClassId, out var type)) {
                    if (overrideGame != UnityGame.Default) {
                        overrideGame = UnityGame.Default;
                        continue;
                    }

                    type = BaseType;
                }

                using var reader = new BiEndianBinaryReader(serializedFile.OpenFile(info, stream), serializedFile.Header.IsBigEndian, true);
                var instance = Activator.CreateInstance(type, reader, info, serializedFile);
                if (instance is not SerializedObject serializedObject) {
                    throw new InvalidOperationException();
                }

                if (reader.Unconsumed > 0 &&
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
