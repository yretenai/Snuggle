using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Game.Unite;

public class UniteTexture2DExtension : ISerialized {
    public int UnknownValue { get; set; }
    public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) { }
    public void Deserialize(ObjectDeserializationOptions options) { }

    public void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) { }
    public void Free() { }
    public (long, string) GetCompositeId() => (0, "Unite");
    public bool ShouldDeserialize => false;
}
