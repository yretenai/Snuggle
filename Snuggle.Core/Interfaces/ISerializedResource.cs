using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Models.Objects;
using Snuggle.Core.Options;

namespace Snuggle.Core.Interfaces {
    [PublicAPI]
    public interface ISerializedResource : ISerialized {
        public StreamingInfo StreamData { get; set; }
        public long StreamDataOffset { get; set; }
        public void Serialize(BiEndianBinaryWriter writer, BiEndianBinaryWriter resourceStream, AssetSerializationOptions options);
    }
}
