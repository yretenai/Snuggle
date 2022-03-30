using System.Runtime.InteropServices;

namespace Snuggle.Core.Models.Objects.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
public record struct Color32(byte R, byte G, byte B, byte A) {
    public static Color32 Zero { get; } = new(0, 0, 0, 1);

    public ColorRGBA ToRGBA() => new ColorRGBA(R / 255f, G / 255f, B / 255f, A / 255f);
}
