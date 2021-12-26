using System.Text.Json.Serialization;

namespace Snuggle.glTF;

public record OcclusionTextureInfo : TextureInfo {
    /// <summary>
    /// A scalar parameter controlling the amount of occlusion applied.
    /// A value of `0.0` means no occlusion. A value of `1.0` means full occlusion.
    /// This value affects the final occlusion value as: <code>1.0 + strength * (&lt;sampled occlusion texture value&gt; - 1.0)</code>
    /// </summary>
    [JsonPropertyName("strength")]
    public double? Strength { get; set; } = 1D;
}
