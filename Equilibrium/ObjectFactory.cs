using System;
using System.Collections.Generic;
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

        public static Dictionary<ClassId, Type> Implementations { get; set; } = new();

        public static void LoadImplementationTypes(Assembly assembly) {
            foreach (var (type, attribute) in assembly.GetExportedTypes()
                .Where(x => x.IsAssignableTo(BaseType))
                .Select(x => (Type: x, Attribute: x.GetCustomAttribute<ObjectImplementationAttribute>()))
                .Where(x => x.Attribute != null)) {
                Implementations[attribute!.ClassId] = type;
            }
        }

        public static SerializedObject GetInstance(Stream stream, UnityObjectInfo info, SerializedFile serializedFile, ClassId? overrideType = null) {
            if (!Implementations.TryGetValue(overrideType ?? info.ClassId, out var type)) {
                type = BaseType;
            }

            using var reader = new BiEndianBinaryReader(serializedFile.OpenFile(info, stream), serializedFile.Header.IsBigEndian, true);
            var instance = Activator.CreateInstance(type, reader, info, serializedFile);
            if (instance is not SerializedObject serializedObject) {
                throw new InvalidOperationException();
            }

            return serializedObject;
        }
    }
}
