using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
/// An animation sampler combines timestamps with a sequence of output values and defines an interpolation algorithm.
/// </summary>
public record AnimationSampler : Property {
    /// <summary>
    /// The index of an accessor containing keyframe timestamps.
    /// The accessor <b>MUST</b> be of scalar type with floating-point components.
    /// The values represent time in seconds with `time[0] &gt;= 0.0`, and strictly increasing values, i.e., `time[n + 1] &gt; time[n]`.
    /// </summary>
    [JsonPropertyName("input")]
    public int Input { get; set; }

    /// <summary>
    /// Interpolation algorithm.
    /// </summary>
    [JsonPropertyName("interpolation")]
    public AnimationInterpolation Interpolation { get; set; }

    /// <summary>
    /// The index of an accessor, containing keyframe output values.
    /// </summary>
    [JsonPropertyName("output")]
    public int Output { get; set; }
}
