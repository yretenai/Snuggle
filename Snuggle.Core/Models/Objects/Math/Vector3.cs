namespace Snuggle.Core.Models.Objects.Math;

public record struct Vector3(float X, float Y, float Z) {
    public static Vector3 Zero { get; } = new(0, 0, 0);
    public static Vector3 One { get; } = new(1, 1, 1);

    public static implicit operator System.Numerics.Vector3?(Vector3 vector) => new System.Numerics.Vector3(vector.X, vector.Y, vector.Z);
}
