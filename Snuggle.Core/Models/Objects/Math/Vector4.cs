using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Math;

[PublicAPI]
public record struct Vector4(float X, float Y, float Z, float W) {
    public static Vector4 Zero { get; } = new(0, 0, 0, 0);

    [JsonIgnore]
    public Vector4 JSONSafe {
        get {
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

            return new Vector4(x, y, z, w);
        }
    }

    public static implicit operator System.Numerics.Vector4?(Vector4 vector) => new System.Numerics.Vector4(vector.X, vector.Y, vector.Z, vector.W);
}
