using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Meta {
    [PublicAPI]
    public interface ISerialized {
        public void Deserialize(BiEndianBinaryReader reader);
        public void Deserialize();
        public void Serialize(BiEndianBinaryWriter writer, UnityVersion? targetVersion);
        public void Free();
    }
}
