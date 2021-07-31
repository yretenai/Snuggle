using Equilibrium.IO;
using Equilibrium.Meta.Options;
using JetBrains.Annotations;

namespace Equilibrium.Meta.Interfaces {
    [PublicAPI]
    public interface ISerializedResource {
        public void Serialize(BiEndianBinaryWriter writer, string fileName, BiEndianBinaryWriter resourceStream, string resourceName, UnityVersion? targetVersion, FileSerializationOptions options);
    }
}
