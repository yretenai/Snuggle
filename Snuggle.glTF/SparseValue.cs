using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
///     An object pointing to a buffer view containing the deviating accessor values. The number of elements is equal
///     to `accessor.sparse.count` times number of components. The elements have the same component type as the base
///     accessor. The elements are tightly packed. Data <b>MUST</b> be aligned following the same rules as the base
///     accessor.
/// </summary>
public record SparseValue : Property {
    /// <summary>
    ///     The index of the bufferView with sparse values. The referenced buffer view <b>MUST NOT</b> have its `target`
    ///     or `byteStride` properties defined.
    /// </summary>
    [JsonPropertyName("bufferView")]
    public int BufferView { get; set; }

    /// <summary>The offset relative to the start of the bufferView in bytes.</summary>
    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; } = 0;
}
