using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects;

[PublicAPI]
public record AssetBundleInfo(byte[] Hash, int[] Dependencies) {
    public static AssetBundleInfo FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var hash = reader.ReadBytes(16);
        var count = reader.ReadInt32();
        var dependencies = reader.ReadArray<int>(count).ToArray();
        return new AssetBundleInfo(hash, dependencies);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.Write(Hash);
        writer.WriteArray(Dependencies);
    }
}
