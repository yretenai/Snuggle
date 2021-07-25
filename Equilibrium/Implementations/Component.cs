using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(ClassId.Component)]
    public class Component : SerializedObject {
        public Component(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) => GameObject = PPtr<GameObject>.FromReader(reader, serializedFile);

        public PPtr<GameObject> GameObject { get; init; }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), GameObject);
    }
}
