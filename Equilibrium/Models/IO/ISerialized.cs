using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.IO {
    [PublicAPI]
    public interface ISerialized {
        public void Deserialize(BiEndianBinaryReader reader);
    }
}
