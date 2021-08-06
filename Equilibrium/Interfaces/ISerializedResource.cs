using Equilibrium.IO;
using Equilibrium.Models.Objects;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Interfaces {
    [PublicAPI]
    public interface ISerializedResource : ISerialized {
        public StreamingInfo StreamData { get; set; }
        public void Serialize(BiEndianBinaryWriter writer, BiEndianBinaryWriter resourceStream, AssetSerializationOptions options);
    }
}
