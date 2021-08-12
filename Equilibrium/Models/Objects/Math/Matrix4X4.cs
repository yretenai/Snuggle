using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Math {
    [PublicAPI]
    public record struct Matrix4X4(float M11, float M12, float M13, float M14, float M21, float M22, float M23, float M24, float M31, float M32, float M33, float M34, float M41, float M42, float M43, float M44) {
        public static Matrix4X4 Zero { get; } = new(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

        public static implicit operator Matrix4x4?(Matrix4X4 matrix) => new Matrix4x4(matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24, matrix.M31, matrix.M32, matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44);

        public float[] GetFloats() {
            return MemoryMarshal.Cast<Matrix4X4, float>(new[] { this }).ToArray();
        }
    }
}
