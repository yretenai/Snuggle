using System;
using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>Texture sampler properties for filtering and wrapping modes.</summary>
public record Sampler : ChildOfRootProperty {
    /// <summary>Magnification filter.</summary>
    [JsonPropertyName("magFilter")]
    public MagnificationFilter? MagnificationFilter { get; set; }

    /// <summary>Minification filter.</summary>
    [JsonPropertyName("minFilter")]
    public MinificationFilter? MinificationFilter { get; set; }

    /// <summary>S (U) wrapping mode.</summary>
    [JsonPropertyName("wrapS")]
    public WrapMode WrapS { get; set; } = WrapMode.Repeat;

    /// <summary>T (V) wrapping mode.</summary>
    [JsonPropertyName("wrapT")]
    public WrapMode WrapT { get; set; } = WrapMode.Repeat;

    public override int GetHashCode() => HashCode.Combine(MagnificationFilter, MinificationFilter, WrapS, WrapT);
}
