using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Math {
    [PublicAPI]
    public record struct Vector4(float X, float Y, float Z, float W) {
        public static Vector4 Zero { get; } = new(0, 0, 0, 0);

        public static implicit operator System.Numerics.Vector4?(Vector4 vector) => new System.Numerics.Vector4(vector.X, vector.Y, vector.Z, vector.W);
    }
}
