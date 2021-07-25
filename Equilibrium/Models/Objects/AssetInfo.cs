using Equilibrium.Implementations;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects {
    [PublicAPI]
    public record AssetInfo(
        int PreloadIndex,
        int PreloadSize,
        PPtr<SerializedObject> Asset) {
        public static AssetInfo FromReader(BiEndianBinaryReader reader, SerializedFile file) => new(reader.ReadInt32(), reader.ReadInt32(), PPtr<SerializedObject>.FromReader(reader, file));
    }

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
