using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
/// A node in the node hierarchy.
/// When the node contains `skin`, all `mesh.primitives` <b>MUST</b> contain `JOINTS_0` and `WEIGHTS_0` attributes.
/// A node <b>MAY</b> have either a `matrix` or any combination of `translation`/`rotation`/`scale` (TRS) properties.
/// TRS properties are converted to matrices and postmultiplied in the `T * R * S` order to compose the transformation matrix;
/// first the scale is applied to the vertices, then the rotation, and then the translation.
/// If none are provided, the transform is the identity.
/// When a node is targeted for animation (referenced by an animation.channel.target), `matrix` <b>MUST NOT</b> be present.
/// </summary>
public record Node : ChildOfRootProperty {
    /// <summary>
    /// The index of the camera referenced by this node.
    /// </summary>
    [JsonPropertyName("camera")]
    public int? Camera { get; set; }

    /// <summary>
    /// The indices of this node's children.
    /// </summary>
    [JsonPropertyName("children")]
    public List<int>? Children { get; set; }

    /// <summary>
    /// The index of the skin referenced by this node.
    /// When a skin is referenced by a node within a scene, all joints used by the skin <b>MUST</b> belong to the same scene.
    /// When defined, `mesh` <b>MUST</b> also be defined.
    /// </summary>
    [JsonPropertyName("skin")]
    public int? Skin { get; set; }

    /// <summary>
    /// A floating-point 4x4 transformation matrix stored in column-major order.
    /// </summary>
    [JsonPropertyName("matrix")]
    public List<double>? Matrix { get; set; }

    /// <summary>
    /// The index of the mesh in this node.
    /// </summary>
    [JsonPropertyName("mesh")]
    public int? Mesh { get; set; }

    /// <summary>
    /// The node's unit quaternion rotation in the order (x, y, z, w), where w is the scalar.
    /// </summary>
    [JsonPropertyName("rotation")]
    public List<double>? Rotation { get; set; }

    /// <summary>
    /// The node's non-uniform scale, given as the scaling factors along the x, y, and z axes.
    /// </summary>
    [JsonPropertyName("scale")]
    public List<double>? Scale { get; set; }

    /// <summary>
    /// The node's translation along the x, y, and z axes.
    /// </summary>
    [JsonPropertyName("translation")]
    public List<double>? Translation { get; set; }

    /// <summary>
    /// The weights of the instantiated morph target.
    /// The number of array elements <b>MUST</b> match the number of morph targets of the referenced mesh.
    /// When defined, `mesh` <b>MUST</b> also be defined.
    /// </summary>
    [JsonPropertyName("weights")]
    public List<double>? Weights { get; set; }
}
