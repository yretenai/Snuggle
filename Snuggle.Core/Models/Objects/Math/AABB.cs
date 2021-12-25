using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Math;

[PublicAPI]
public record struct AABB(Vector3 Center, Vector3 Extent) {
    public static AABB Default { get; } = new(Vector3.Zero, Vector3.Zero);

    [JsonIgnore]
    public AABB JSONSafe {
        get {
            var (x, y) = this;
            return new AABB(x.JSONSafe, y.JSONSafe);
        }
    }
}
