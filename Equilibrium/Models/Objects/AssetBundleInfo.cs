using Equilibrium.IO;
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
    }
}
