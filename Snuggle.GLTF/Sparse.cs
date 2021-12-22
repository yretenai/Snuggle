using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
/// Sparse storage of accessor values that deviate from their initialization value.
/// </summary>
public record Sparse : Property {
    /// <summary>
    /// Number of deviating accessor values stored in the sparse array.
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// An object pointing to a buffer view containing the indices of deviating accessor values.
    /// The number of indices is equal to `count`.
    /// Indices <b>MUST</b> strictly increase.
    /// </summary>
    [JsonPropertyName("indices")]
    public SparseIndex Indices { get; set; } = new();

    /// <summary>
    /// An object pointing to a buffer view containing the deviating accessor values.
    /// </summary>
    [JsonPropertyName("values")]
    public SparseValue Values { get; set; } = new();
}
