using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Math; 

[PublicAPI]
public record struct Vector2(float X, float Y) {
    public static Vector2 Zero { get; } = new(0, 0);

    public static implicit operator System.Numerics.Vector2?(Vector2 vector) => new System.Numerics.Vector2(vector.X, vector.Y);
}