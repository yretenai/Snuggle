using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Graphics {
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
