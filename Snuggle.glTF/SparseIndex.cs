using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
///     An object pointing to a buffer view containing the indices of deviating accessor values. The number of indices
///     is equal to `accessor.sparse.count`. Indices <b>MUST</b> strictly increase.
/// </summary>
public record SparseIndex : Property {
    /// <summary>
    ///     The index of the buffer view with sparse indices. The referenced buffer view <b>MUST NOT</b> have its `target`
    ///     or `byteStride` properties defined. The buffer view and the optional `byteOffset` <b>MUST</b> be aligned to the
    ///     `componentType` byte length.
    /// </summary>
    [JsonPropertyName("bufferView")]
    public int BufferView { get; set; }

    /// <summary>The offset relative to the start of the buffer view in bytes.</summary>
    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; } = 0;

    /// <summary>The indices data type.</summary>
    [JsonPropertyName("componentType")]
    public SparseIndexType ComponentType { get; set; }
}
