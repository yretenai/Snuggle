using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
/// An orthographic camera containing properties to create an orthographic projection matrix.
/// </summary>
public record OrthographicCamera : Property {
    /// <summary>
    /// The floating-point horizontal magnification of the view.
    /// This value <b>MUST NOT</b> be equal to zero.
    /// This value <b>SHOULD NOT</b> be negative.
    /// </summary>
    [JsonPropertyName("xmag")]
    public double XMagnification { get; set; }

    /// <summary>
    /// The floating-point vertical magnification of the view.
    /// This value <b>MUST NOT</b> be equal to zero.
    /// This value <b>SHOULD NOT</b> be negative.
    /// </summary>
    [JsonPropertyName("ymag")]
    public double YMagnification { get; set; }

    /// <summary>
    /// The floating-point distance to the far clipping plane.
    /// This value <b>MUST NOT</b> be equal to zero.
    /// `zfar` <b>MUST</b> be greater than `znear`.
    /// </summary>
    [JsonPropertyName("zfar")]
    public double FarPlane { get; set; }

    /// <summary>
    /// The floating-point distance to the near clipping plane.
    /// </summary>
    [JsonPropertyName("znear")]
    public double NearPlane { get; set; }
}
