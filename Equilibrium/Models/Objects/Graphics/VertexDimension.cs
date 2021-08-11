using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public enum VertexDimension : byte {
        None = 0,
        R = 1,
        RG = 2,
        RGB = 3,
        RGBA = 4,
    }
}
