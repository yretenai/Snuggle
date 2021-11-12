using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Math;

[PublicAPI]
public record struct ColorRGBA(float R, float G, float B, float A) {
    public static ColorRGBA Zero { get; } = new(0, 0, 0, 1);
}
