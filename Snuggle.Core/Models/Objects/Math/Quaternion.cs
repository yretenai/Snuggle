using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Math;

[PublicAPI]
public record struct Quaternion(float X, float Y, float Z, float W) {
    public static Quaternion Zero { get; } = new(0, 0, 0, 1);

    public static implicit operator System.Numerics.Quaternion?(Quaternion quaternion) => new System.Numerics.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
}
