using JetBrains.Annotations;

namespace Equilibrium.Models.IO {
    [PublicAPI]
    public interface IReversibleStruct {
        public void ReverseEndianness();
    }
}
