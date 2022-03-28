namespace Snuggle.Core.Models.Objects.Math;

public record struct Quaternion(float X, float Y, float Z, float W) {
    public static Quaternion Zero { get; } = new(0, 0, 0, 1);

    public Quaternion GetJSONSafe() {
        var (x, y, z, w) = this;
        if (!float.IsNormal(x)) {
            x = 1;
        }

        if (!float.IsNormal(y)) {
            y = 1;
        }

        if (!float.IsNormal(z)) {
            z = 1;
        }

        if (!float.IsNormal(w)) {
            w = 1;
        }

        return new Quaternion(x, y, z, w);
    }

    public static implicit operator System.Numerics.Quaternion?(Quaternion quaternion) => new System.Numerics.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
}
