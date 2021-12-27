using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>A typed view into a buffer view that contains raw binary data.</summary>
public record Accessor : ChildOfRootProperty {
    /// <summary>
    ///     The index of the buffer view. When undefined, the accessor <b>MUST</b> be initialized with zeros; `sparse`
    ///     property or extensions <b>MAY</b> override zeros with actual values.
    /// </summary>
    [JsonPropertyName("bufferView")]
    public int? BufferView { get; set; }

    /// <summary>
    ///     The offset relative to the start of the buffer view in bytes. This <b>MUST</b> be a multiple of the size of
    ///     the component datatype. This property <b>MUST NOT</b> be defined when `bufferView` is undefined.
    /// </summary>
    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; } = 0;

    /// <summary>
    ///     The datatype of the accessor's components. UNSIGNED_INT type <b>MUST NOT</b> be used for any accessor that is
    ///     not referenced by `mesh.primitive.ndices`.
    /// </summary>
    [JsonPropertyName("componentType")]
    public AccessorComponentType ComponentType { get; set; }

    /// <summary>
    ///     Specifies whether integer data values are normalized (`true`) to [0, 1] (for unsigned types) or to [-1, 1]
    ///     (for signed types) when they are accessed. This property <b>MUST NOT</b> be set to `true` for accessors with
    ///     `FLOAT` or `UNSIGNED_INT` component type.
    /// </summary>
    [JsonPropertyName("normalized")]
    public bool? Normalized { get; set; }

    /// <summary>
    ///     The number of elements referenced by this accessor, not to be confused with the number of bytes or number of
    ///     components.
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>Specifies if the accessor's elements are scalars, vectors, or matrices.</summary>
    [JsonPropertyName("type")]
    public AccessorType Type { get; set; }

    /// <summary>
    ///     Maximum value of each component in this accessor. Array elements <b>MUST</b> be treated as having the same
    ///     data type as accessor's `componentType`. Both `min` and `max` arrays have the same length. The length is determined
    ///     by the value of the `type` property; it can be 1, 2, 3, 4, 9, or 16. `normalized` property has no effect on array
    ///     values: they always correspond to the actual values stored in the buffer. When the accessor is sparse, this
    ///     property <b>MUST</b> contain maximum values of accessor data with sparse substitution applied.
    /// </summary>
    [JsonPropertyName("max")]
    public List<double>? Max { get; set; }

    /// <summary>
    ///     Minimum value of each component in this accessor. Array elements <b>MUST</b> be treated as having the same
    ///     data type as accessor's `componentType`. Both `min` and `max` arrays have the same length. The length is determined
    ///     by the value of the `type` property; it can be 1, 2, 3, 4, 9, or 16. `normalized` property has no effect on array
    ///     values: they always correspond to the actual values stored in the buffer. When the accessor is sparse, this
    ///     property <b>MUST</b> contain minimum values of accessor data with sparse substitution applied.
    /// </summary>
    [JsonPropertyName("min")]
    public List<double>? Min { get; set; }

    /// <summary>Sparse storage of elements that deviate from their initialization value.</summary>
    [JsonPropertyName("sparse")]
    public Sparse? Sparse { get; set; }
}
