using JetBrains.Annotations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Game.Unite; 

[PublicAPI]
public class UniteTexture2DExtension : ISerialized {
    public int UnknownValue { get; set; }
    public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) { }

    public void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) { }
    public void Free() { }
}