using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Objects.Math;

namespace Snuggle.Core.Models.Objects.Graphics;

[PublicAPI]
public record SpriteBone(string Name, Vector3 Position, Quaternion Rotation, float Length, int ParentId) {
    public static SpriteBone Default { get; set; } = new(string.Empty, Vector3.Zero, Quaternion.Zero, 1, -1);

    public static SpriteBone FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var name = reader.ReadString32();
        var pos = reader.ReadStruct<Vector3>();
        var rot = reader.ReadStruct<Quaternion>();
        var length = reader.ReadSingle();
        var parent = reader.ReadInt32();
        return new SpriteBone(name, pos, rot, length, parent);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.WriteString32(Name);
        writer.WriteStruct(Position);
        writer.WriteStruct(Rotation);
        writer.Write(Length);
        writer.Write(ParentId);
    }
}
