using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Interfaces;

[PublicAPI]
public interface ISerialized {
    public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options);
    public void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options);
    public void Free();
}
