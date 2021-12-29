using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Math;

[PublicAPI]
public record struct Vector3(float X, float Y, float Z) {
    public static Vector3 Zero { get; } = new(0, 0, 0);
    public static Vector3 One { get; } = new(1, 1, 1);

    public Vector3 GetJSONSafe() {
        var (x, y, z) = this;
        if (!float.IsNormal(x)) {
            x = 1;
        }

        if (!float.IsNormal(y)) {
            y = 1;
        }

        if (!float.IsNormal(z)) {
            z = 1;
        }

        return new Vector3(x, y, z);
    }

    public static implicit operator System.Numerics.Vector3?(Vector3 vector) => new System.Numerics.Vector3(vector.X, vector.Y, vector.Z);
}
