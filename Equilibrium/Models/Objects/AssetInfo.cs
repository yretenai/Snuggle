using Equilibrium.Implementations;
using Equilibrium.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects {
    [PublicAPI]
    public record AssetInfo(
        int PreloadIndex,
        int PreloadSize,
        PPtr<SerializedObject> Asset) {
        public static AssetInfo Default { get; } = new(0, 0, PPtr<SerializedObject>.Null);
        
        public static AssetInfo FromReader(BiEndianBinaryReader reader, SerializedFile file) => new(reader.ReadInt32(), reader.ReadInt32(), PPtr<SerializedObject>.FromReader(reader, file));

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            writer.Write(PreloadIndex);
            writer.Write(PreloadSize);
            Asset.ToWriter(writer, serializedFile, targetVersion);
        }
    }
}
