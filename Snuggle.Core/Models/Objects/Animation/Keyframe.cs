using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Animation;

public record Keyframe<T>(float Time, T Value, T InSlope, T OutSlope, int WeightedMode, T InWeight, T OutWeight) where T : struct {
    public static Keyframe<T> FromReader(BiEndianBinaryReader reader, SerializedFile serializedFile) {
        var time = reader.ReadSingle();
        var value = reader.ReadStruct<T>();
        var inS = reader.ReadStruct<T>();
        var outS = reader.ReadStruct<T>();
        var weightedMode = -1;
        var inW = default(T);
        var outW = default(T);
        if (serializedFile.Version >= UnityVersionRegister.Unity2018) {
            weightedMode = reader.ReadInt32();
            inW = reader.ReadStruct<T>();
            outW = reader.ReadStruct<T>();
        }

        return new Keyframe<T>(time, value, inS, outS, weightedMode, inW, outW);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) { }
}
