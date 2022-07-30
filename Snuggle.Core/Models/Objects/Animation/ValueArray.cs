using System.Collections.Generic;
using System.Linq;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Animation;

public record ValueArray(List<bool> BoolValues, List<int> IntValues, List<float> FloatValues, List<Vector3> PositionValues, List<Quaternion> RotationValues, List<Vector3> ScaleValues) {
    public static ValueArray Default { get; } = new(new List<bool>(), new List<int>(), new List<float>(), new List<Vector3>(), new List<Quaternion>(), new List<Vector3>());

    public static ValueArray FromReader(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        var bools = new List<bool>();
        var ints = new List<int>();
        var floats = new List<float>();
        var positions = new List<Vector3>();
        var rotations = new List<Quaternion>();
        var scales = new List<Vector3>();

        bools.AddRange(reader.ReadArray<bool>(reader.ReadInt32()));
        reader.Align();

        ints.AddRange(reader.ReadArray<int>(reader.ReadInt32()));
        reader.Align();

        floats.AddRange(reader.ReadArray<float>(reader.ReadInt32()));
        reader.Align();

        positions.AddRange(reader.ReadArray<Vector4>(reader.ReadInt32()).Select(x => new Vector3(x.X, x.Y, x.Z)));
        reader.Align();

        rotations.AddRange(reader.ReadArray<Quaternion>(reader.ReadInt32()));
        reader.Align();

        scales.AddRange(reader.ReadArray<Vector4>(reader.ReadInt32()).Select(x => new Vector3(x.X, x.Y, x.Z)));
        reader.Align();

        return new ValueArray(bools, ints, floats, positions, rotations, scales);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.WriteArray(BoolValues);
        writer.WriteArray(IntValues);
        writer.WriteArray(FloatValues);
        writer.WriteArray(PositionValues.Select(x => new Vector4(x.X, x.Y, x.Z, 0)));
        writer.WriteArray(RotationValues);
        writer.WriteArray(ScaleValues.Select(x => new Vector4(x.X, x.Y, x.Z, 0)));
    }
}
