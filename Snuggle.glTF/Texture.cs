using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>A texture and its sampler.</summary>
public record Texture : ChildOfRootProperty {
    /// <summary>
    ///     The index of the sampler used by this texture. When undefined, a sampler with repeat wrapping and auto
    ///     filtering <b>SHOULD</b> be used.
    /// </summary>
    [JsonPropertyName("sampler")]
    public int? Sampler { get; set; }

    /// <summary>
    ///     The index of the image used by this texture. When undefined, an extension or other mechanism <b>SHOULD</b>
    ///     supply an alternate texture source, otherwise behavior is undefined.
    /// </summary>
    [JsonPropertyName("source")]
    public int? Source { get; set; }
}
