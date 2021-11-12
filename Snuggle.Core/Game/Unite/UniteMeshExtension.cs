using JetBrains.Annotations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Game.Unite;

[PublicAPI]
public class UniteMeshExtension : ISerialized {
    public int BoneCount { get; set; }
    public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) { }

    public void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) { }
    public void Free() { }
}

[PublicAPI]
public class UniteAssetBundleExtension : ISerialized {
    public uint Unknown1 { get; set; }
    public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) { }

    public void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) { }
    public void Free() { }
}
