using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Game.Unite;

public class UniteMeshExtension : ISerialized {
    public int BoneCount { get; set; }
    public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) { }
    public void Deserialize(ObjectDeserializationOptions options) { }

    public void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) { }
    public void Free() { }
    public bool ShouldDeserialize => false;
    public (long, string) GetCompositeId() => (0, "Unite");
}
