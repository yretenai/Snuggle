using System.Numerics;
using System.Runtime.InteropServices;

namespace Snuggle.Core.Models.Objects.Math;

public record struct Matrix4X4(Vector4 M1, Vector4 M2, Vector4 M3, Vector4 M4) {
    public static Matrix4X4 Zero { get; } = new(new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1));

    public float[] GetFloats() {
        return MemoryMarshal.Cast<Matrix4X4, float>(new[] { this }).ToArray();
    }

    public Matrix4x4 GetNumerics() =>
        new(M1.X, M1.Y, M1.Z, M1.W, M2.X, M2.Y, M2.Z, M2.W, M3.X, M3.Y, M3.Z, M3.W, M4.X, M4.Y, M4.Z, M4.W);
}
