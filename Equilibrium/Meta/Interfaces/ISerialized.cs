using Equilibrium.IO;
using Equilibrium.Meta.Options;
using JetBrains.Annotations;

namespace Equilibrium.Meta.Interfaces {
    [PublicAPI]
    public interface ISerialized {
        public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options);
        public void Deserialize(ObjectDeserializationOptions options);
        public void Serialize(BiEndianBinaryWriter writer, string fileName, UnityVersion? targetVersion, FileSerializationOptions options);
        public void Free();
    }
}
