namespace Snuggle.Core.Models.Objects.Math;

public record struct Vector2(float X, float Y) {
    public static Vector2 Zero { get; } = new(0, 0);

    public Vector2 GetJSONSafe() {
        var (x, y) = this;

        if (!float.IsNormal(x)) {
            x = 0;
        }

        if (!float.IsNormal(y)) {
            y = 0;
        }

        return new Vector2(x, y);
    }

    public static implicit operator System.Numerics.Vector2?(Vector2 vector) => new System.Numerics.Vector2(vector.X, vector.Y);
}
