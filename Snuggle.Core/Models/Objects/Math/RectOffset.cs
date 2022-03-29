namespace Snuggle.Core.Models.Objects.Math;

public record struct RectOffset(int Left, int Right, int Top, int Bottom) {
    public static RectOffset Zero { get; } = new(0, 0, 0, 0);
}
