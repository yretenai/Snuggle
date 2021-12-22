using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
/// An animation channel combines an animation sampler with a target property being animated.
/// </summary>
public record AnimationChannel : Property {
    /// <summary>
    /// The index of a sampler in this animation used to compute the value for the target, e.g., a node's translation, rotation, or scale (TRS).
    /// </summary>
    [JsonPropertyName("sampler")]
    public int Sampler { get; set; }

    /// <summary>
    /// The descriptor of the animated property.
    /// </summary>
    [JsonPropertyName("target")]
    public AnimationChannelTarget Target { get; set; } = new();
}
