using System;
using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Math;

[PublicAPI]
public record struct Matrix4X4(
    float M11,
    float M21,
    float M31,
    float M41,
    float M12,
    float M22,
    float M32,
    float M42,
    float M13,
    float M23,
    float M33,
    float M43,
    float M14,
    float M24,
    float M34,
    float M44) {
    public static Matrix4X4 Zero { get; } = new(
        1,
        0,
        0,
        0,
        0,
        1,
        0,
        0,
        0,
        0,
        1,
        0,
        0,
        0,
        0,
        1);

    public Matrix4X4 GetJSONSafe() {
        Span<float> floats = GetFloats();
        for (var i = 0; i < floats.Length; ++i) {
            if (!float.IsNormal(floats[i])) {
                floats[i] = 0;
            }
        }

        return MemoryMarshal.Cast<float, Matrix4X4>(floats)[0];
    }

    public float[] GetFloats() {
        return MemoryMarshal.Cast<Matrix4X4, float>(new[] { this }).ToArray();
    }

    public Matrix4x4 GetNumerics() =>
        new(
            M11,
            M12,
            M13,
            M14,
            M21,
            M22,
            M23,
            M24,
            M31,
            M32,
            M33,
            M34,
            M41,
            M42,
            M43,
            M44);
}
