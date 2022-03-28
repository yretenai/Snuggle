namespace Snuggle.Core.Models.Objects.Math;

public record struct AABB(Vector3 Center, Vector3 Extent) {
    public static AABB Default { get; } = new(Vector3.Zero, Vector3.Zero);

    public AABB GetJSONSafe() {
        var (x, y) = this;
        return new AABB(x.GetJSONSafe(), y.GetJSONSafe());
    }
}
