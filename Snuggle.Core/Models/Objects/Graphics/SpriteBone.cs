using System;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Objects.Math;

namespace Snuggle.Core.Models.Objects.Graphics;

[PublicAPI]
public record SpriteBone(string Name, Guid Guid, Vector3 Position, Quaternion Rotation, float Length, int ParentId, ColorRGBA Color) {
    public static SpriteBone Default { get; set; } = new(string.Empty, Guid.Empty, Vector3.Zero, Quaternion.Zero, 1, -1, ColorRGBA.Zero);

    public static SpriteBone FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var name = reader.ReadString32();
        var guid = file.Version >= UnityVersionRegister.Unity2021_1 ? reader.ReadStruct<Guid>() : Guid.Empty;
        var pos = reader.ReadStruct<Vector3>();
        var rot = reader.ReadStruct<Quaternion>();
        var length = reader.ReadSingle();
        var parent = reader.ReadInt32();
        var color = file.Version >= UnityVersionRegister.Unity2021_1 ? reader.ReadStruct<ColorRGBA>() : ColorRGBA.Zero;
        return new SpriteBone(name, guid, pos, rot, length, parent, color);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.WriteString32(Name);
        if (targetVersion >= UnityVersionRegister.Unity2021_1) {
            writer.WriteStruct(Guid);
        }
        writer.WriteStruct(Position);
        writer.WriteStruct(Rotation);
        writer.Write(Length);
        writer.Write(ParentId);
        if (targetVersion >= UnityVersionRegister.Unity2021_1) {
            writer.WriteStruct(Color);
        }
    }
}
