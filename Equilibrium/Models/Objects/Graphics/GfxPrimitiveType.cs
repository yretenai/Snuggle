using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public enum GfxPrimitiveType {
        Triangles,
        TriangleStrip,
        Quads,
        Lines,
        Strip,
        Points,
    }
}
