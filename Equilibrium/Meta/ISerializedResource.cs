using System.IO;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Meta {
    [PublicAPI]
    public interface ISerializedResource {
        public Stream GetStream { get; set; }
        public void Deserialize(BiEndianBinaryReader reader, BiEndianBinaryReader resourceStream);
        public void Serialize(BiEndianBinaryWriter writer, BiEndianBinaryWriter resourceStream, UnityVersion? version);
    }
}
