using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
/// Metadata about the glTF asset.
/// </summary>
public record Asset : Property {
    /// <summary>
    /// A copyright message suitable for display to credit the content creator.
    /// </summary>
    [JsonPropertyName("copyright")]
    public string? Copyright { get; set; }

    /// <summary>
    /// Tool that generated this glTF model.  Useful for debugging.
    /// </summary>
    [JsonPropertyName("generator")]
    public string Generator { get; set; } = "Snuggle.glTF";

    /// <summary>
    /// The glTF version in the form of `&lt;major&gt;.&lt;minor&gt;` that this asset targets.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "2.1";

    /// <summary>
    /// The minimum glTF version in the form of `&lt;major&gt;.&lt;minor&gt;` that this asset targets.
    /// This property <b>MUST NOT</b> be greater than the asset version.
    /// </summary>
    [JsonPropertyName("minVersion")]
    public string MinVersion { get; set; } = "2.0";
}
