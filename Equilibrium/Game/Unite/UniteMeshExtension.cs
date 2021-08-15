﻿using Equilibrium.Interfaces;
using Equilibrium.IO;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Game.Unite {
    [PublicAPI]
    public class UniteMeshExtension : ISerialized {
        public int BoneCount { get; set; }
        public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) { }

        public void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) { }
        public void Free() { }
    }
}