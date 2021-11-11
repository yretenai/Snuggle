using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Math {
    [PublicAPI]
    public record struct AABB(
        Vector3 Center,
        Vector3 Extent) {
        public static AABB Default { get; } = new(Vector3.Zero, Vector3.Zero);
    }
}
