using System.Numerics;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Math {
    [PublicAPI]
    public record struct Vector2(float X, float Y) {
        public static Vector2 Zero { get; } = new(0, 0);

        public static implicit operator System.Numerics.Vector2?(Vector2 vector) => new System.Numerics.Vector2(vector.X, vector.Y);
    }

    [PublicAPI]
    public record struct Vector3(float X, float Y, float Z) {
        public static Vector3 Zero { get; } = new(0, 0, 0);

        public static implicit operator System.Numerics.Vector3?(Vector3 vector) => new System.Numerics.Vector3(vector.X, vector.Y, vector.Z);
    }

    [PublicAPI]
    public record struct Vector4(float X, float Y, float Z, float W) {
        public static Vector4 Zero { get; } = new(0, 0, 0, 0);

        public static implicit operator System.Numerics.Vector4?(Vector4 vector) => new System.Numerics.Vector4(vector.X, vector.Y, vector.Z, vector.W);
    }

    [PublicAPI]
    public record struct Rect(float X, float Y, float W, float H) {
        public static Rect Zero { get; } = new(0, 0, 0, 0);
    }

    [PublicAPI]
    public record struct ColorRGBA(float R, float G, float B, float A) {
        public static ColorRGBA Zero { get; } = new(0, 0, 0, 1);
    }

    [PublicAPI]
    public record struct Quaternion(float X, float Y, float Z, float W) {
        public static Quaternion Zero { get; } = new(0, 0, 0, 1);

        public static implicit operator System.Numerics.Quaternion?(Quaternion quaternion) => new System.Numerics.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
    }

    [PublicAPI]
    public record struct Matrix4X4(float M11, float M12, float M13, float M14, float M21, float M22, float M23, float M24, float M31, float M32, float M33, float M34, float M41, float M42, float M43, float M44) {
        public static Matrix4X4 Zero { get; } = new(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

        public static implicit operator Matrix4x4?(Matrix4X4 matrix) => new Matrix4x4(matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24, matrix.M31, matrix.M32, matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44);
    }
}
