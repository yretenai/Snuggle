using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Graphics;

[PublicAPI]
public record GLTextureSettings(
    FilterMode FilterMode,
    int Asiostropy,
    float Bias,
    TextureWrapMode WrapU,
    TextureWrapMode WrapV,
    TextureWrapMode WrapW) {
    public static GLTextureSettings Default { get; } = new(FilterMode.Point, 0, 0, TextureWrapMode.Repeat, TextureWrapMode.Repeat, TextureWrapMode.Repeat);

    public static GLTextureSettings FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var filterMode = (FilterMode) reader.ReadInt32();
        var anisotropy = reader.ReadInt32();
        var bias = reader.ReadSingle();
        var wrapu = (TextureWrapMode) reader.ReadInt32();
        var wrapv = wrapu;
        var wrapw = wrapu;
        if (file.Version >= UnityVersionRegister.Unity2017_1) {
            wrapv = (TextureWrapMode) reader.ReadInt32();
            wrapw = (TextureWrapMode) reader.ReadInt32();
        }

        return new GLTextureSettings(filterMode, anisotropy, bias, wrapu, wrapv, wrapw);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.Write((int) FilterMode);
        writer.Write(Asiostropy);
        writer.Write(Bias);
        writer.Write((int) WrapU);

        if (targetVersion >= UnityVersionRegister.Unity2017_1) {
            writer.Write((int) WrapV);
            writer.Write((int) WrapW);
        }
    }
}
