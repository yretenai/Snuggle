﻿using System.Numerics;
using Equilibrium.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Math {
    [PublicAPI]
    public record AABB(
        Vector3 Center,
        Vector3 Extent) {
        public static AABB Default { get; } = new(Vector3.Zero, Vector3.Zero);

        public static AABB FromReader(BiEndianBinaryReader reader, SerializedFile file) =>
            new(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            writer.Write(Center.X);
            writer.Write(Center.Y);
            writer.Write(Center.Z);
            writer.Write(Extent.X);
            writer.Write(Extent.Y);
            writer.Write(Extent.Z);
        }
    }
}