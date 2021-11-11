using JetBrains.Annotations;
using Snuggle.Core.Implementations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects {
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
