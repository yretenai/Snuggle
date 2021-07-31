using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public enum TextureDimension {
        Unknown = -1,
        None = 0,
        Texture1D = 1,
        Texture2D = 2,
        Texture3D = 3,
        Cubemap = 4,
        Texture2DArray = 5,
        CubemapArray = 6,
    }
}
