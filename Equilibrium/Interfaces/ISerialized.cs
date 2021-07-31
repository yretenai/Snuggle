using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Interfaces {
    [PublicAPI]
    public interface ISerialized {
        public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options);
        public void Deserialize(ObjectDeserializationOptions options);
        public void Serialize(BiEndianBinaryWriter writer, string fileName, UnityVersion? targetVersion, FileSerializationOptions options);
        public void Free();
    }
}
