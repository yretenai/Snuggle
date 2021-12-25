using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Math;

[PublicAPI]
public record struct ColorRGBA(float R, float G, float B, float A) {
    public static ColorRGBA Zero { get; } = new(0, 0, 0, 1);

    [JsonIgnore]
    public ColorRGBA JSONSafe {
        get {
            var (r, g, b, a) = this;
            if (!float.IsNormal(r)) {
                r = 1;
            }

            if (!float.IsNormal(b)) {
                b = 1;
            }

            if (!float.IsNormal(g)) {
                g = 1;
            }

            if (!float.IsNormal(a)) {
                a = 1;
            }

            return new ColorRGBA(r, g, b, a);
        }
    }
}
