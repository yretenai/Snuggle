using JetBrains.Annotations;

namespace Equilibrium.Models {
    [PublicAPI]
    public interface IReversibleStruct {
        public void ReverseEndianness();
    }
}
