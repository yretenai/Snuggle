using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
/// Joints and matrices defining a skin.
/// </summary>
public record Skin : ChildOfRootProperty {
    /// <summary>
    /// The index of the accessor containing the floating-point 4x4 inverse-bind matrices.
    /// Its `accessor.count` property <b>MUST</b> be greater than or equal to the number of elements of the `joints` array.
    /// When undefined, each matrix is a 4x4 identity matrix.
    /// </summary>
    [JsonPropertyName("inverseBindMatrices")]
    public int? InverseBindMatrices { get; set; }

    /// <summary>
    /// The index of the node used as a skeleton root.
    /// The node <b>MUST</b> be the closest common root of the joints hierarchy or a direct or indirect parent node of the closest common root.
    /// </summary>
    [JsonPropertyName("skeleton")]
    public int? Skeleton { get; set; }

    /// <summary>
    /// Indices of skeleton nodes, used as joints in this skin.
    /// </summary>
    [JsonPropertyName("joints")]
    public List<int> Joints { get; set; } = new();
}
