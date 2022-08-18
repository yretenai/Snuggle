using System;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Animation;

public record ValueArray(bool[] BoolValues, int[] IntValues, float[] FloatValues, Vector4[] PositionValues, Quaternion[] RotationValues, Vector4[] ScaleValues) {
    public static ValueArray Default { get; } = new(Array.Empty<bool>(), Array.Empty<int>(), Array.Empty<float>(), Array.Empty<Vector4>(), Array.Empty<Quaternion>(), Array.Empty<Vector4>());

    public static ValueArray FromReader(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        var bools = reader.ReadArray<bool>(reader.ReadInt32());
        reader.Align();

        var ints = reader.ReadArray<int>(reader.ReadInt32());
        var floats = reader.ReadArray<float>(reader.ReadInt32());
        var positions = reader.ReadArray<Vector4>(reader.ReadInt32());
        var rotations = reader.ReadArray<Quaternion>(reader.ReadInt32());
        var scales = reader.ReadArray<Vector4>(reader.ReadInt32());

        return new ValueArray(bools, ints, floats, positions, rotations, scales);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.WriteArray(BoolValues);
        writer.WriteArray(IntValues);
        writer.WriteArray(FloatValues);
        writer.WriteArray(PositionValues);
        writer.WriteArray(RotationValues);
        writer.WriteArray(ScaleValues);
    }
}
