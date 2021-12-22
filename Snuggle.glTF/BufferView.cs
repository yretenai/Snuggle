using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
/// A view into a buffer generally representing a subset of the buffer.
/// </summary>
public record BufferView : ChildOfRootProperty {
    /// <summary>
    /// The index of the buffer.
    /// </summary>
    [JsonPropertyName("buffer")]
    public int Buffer { get; set; }

    /// <summary>
    /// The offset into the buffer in bytes.
    /// </summary>
    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; } = 0;

    /// <summary>
    /// The length of the bufferView in bytes.
    /// </summary>
    [JsonPropertyName("byteLength")]
    public int ByteLength { get; set; }

    /// <summary>
    /// The stride, in bytes, between vertex attributes.
    /// When this is not defined, data is tightly packed.
    /// When two or more accessors use the same buffer view, this field <b>MUST</b> be defined.
    /// </summary>
    [JsonPropertyName("byteStride")]
    public int? ByteStride { get; set; }

    /// <summary>
    /// The hint representing the intended GPU buffer type to use with this buffer view.
    /// </summary>
    [JsonPropertyName("target")]
    public BufferViewTarget Target { get; set; }
}
