using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Interfaces {
    [PublicAPI]
    public interface ISerializedResource {
        public void Serialize(BiEndianBinaryWriter writer, string fileName, BiEndianBinaryWriter resourceStream, string resourceName, UnityVersion? targetVersion, FileSerializationOptions options);
    }
}
