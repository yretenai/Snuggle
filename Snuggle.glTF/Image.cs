using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>Image data used to create a texture. Image <b>MAY</b> be referenced by an URI (or IRI) or a buffer view index.</summary>
public record Image : ChildOfRootProperty {
    /// <summary>
    ///     The URI (or IRI) of the image. Relative paths are relative to the current glTF asset. Instead of referencing
    ///     an external file, this field <b>MAY</b> contain a `data:`-URI. This field <b>MUST NOT</b> be defined when
    ///     `bufferView` is defined.
    /// </summary>
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    /// <summary>The image's media type. This field <b>MUST</b> be defined when `bufferView` is defined.</summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    /// <summary>
    ///     The index of the bufferView that contains the image. This field <b>MUST NOT</b> be defined when `uri` is
    ///     defined.
    /// </summary>
    [JsonPropertyName("bufferView")]
    public int? BufferView { get; set; }
}
