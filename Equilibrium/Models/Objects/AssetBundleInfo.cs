using Equilibrium.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects {
    [PublicAPI]
    public record AssetBundleInfo(
        byte[] Hash,
        int[] Dependencies) {
        public static AssetBundleInfo FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var hash = reader.ReadBytes(16);
            var count = reader.ReadInt32();
            var dependencies = reader.ReadArray<int>(count).ToArray();
            return new AssetBundleInfo(hash, dependencies);
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            writer.Write(Hash);
            writer.Write(Dependencies.Length);
            writer.WriteArray(Dependencies);
        }
    }
}
