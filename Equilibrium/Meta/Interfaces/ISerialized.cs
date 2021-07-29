using Equilibrium.IO;
using Equilibrium.Meta.Options;
using JetBrains.Annotations;

namespace Equilibrium.Meta.Interfaces {
    [PublicAPI]
    public interface ISerialized {
        public void Deserialize(BiEndianBinaryReader reader);
        public void Deserialize();
        public void Serialize(BiEndianBinaryWriter writer, UnityVersion? targetVersion, FileSerializationOptions options);
        public void Free();
    }
}
