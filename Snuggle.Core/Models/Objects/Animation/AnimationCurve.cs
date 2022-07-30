using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Animation;

public record AnimationCurve<T>(Keyframe<T>[] Frames, int PreInfinity, int PostInfinity, int RotationOrder) where T : struct {
    public static AnimationCurve<T> FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var count = reader.ReadInt32();
        var frames = new Keyframe<T>[count];
        for (var i = 0; i < count; ++i) {
            frames[i] = Keyframe<T>.FromReader(reader, file);
        }

        var preInfinity = reader.ReadInt32();
        var postInfinity = reader.ReadInt32();
        var order = -1;
        if (file.Version >= UnityVersionRegister.Unity5_3) {
            order = reader.ReadInt32();
        }

        return new AnimationCurve<T>(frames, preInfinity, postInfinity, order);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.Write(Frames.Length);
        foreach (var frame in Frames) {
            frame.ToWriter(writer, serializedFile, targetVersion);
        }

        writer.Write(PreInfinity);
        writer.Write(PostInfinity);
        if (targetVersion >= UnityVersionRegister.Unity5_3) {
            writer.Write(RotationOrder);
        }
    }
}
