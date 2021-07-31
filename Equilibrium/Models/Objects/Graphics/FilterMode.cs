using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public enum FilterMode {
        Point = 0,
        Bilinear = 1,
        Trilinear = 2,
    }
}
