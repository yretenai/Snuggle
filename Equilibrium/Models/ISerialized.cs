using JetBrains.Annotations;

namespace Equilibrium.Models {
    [PublicAPI]
    public interface ISerialized {
        public void Deserialize(BiEndianBinaryReader reader);
    }
}
