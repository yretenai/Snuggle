namespace Snuggle.Core.Models.Objects.Math;

public record struct Rect(float X, float Y, float W, float H) {
    public static Rect Zero { get; } = new(0, 0, 0, 0);

    public Rect GetJSONSafe() {
        var (x, y, z, w) = this;
        if (!float.IsNormal(x)) {
            x = 1;
        }

        if (!float.IsNormal(y)) {
            y = 1;
        }

        if (!float.IsNormal(z)) {
            z = 1;
        }

        if (!float.IsNormal(w)) {
            w = 1;
        }

        return new Rect(x, y, z, w);
    }
}
