using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
///     A camera's projection. A node <b>MAY</b> reference a camera to apply a transform to place the camera in the
///     scene.
/// </summary>
public record Camera : ChildOfRootProperty {
    /// <summary>
    ///     An orthographic camera containing properties to create an orthographic projection matrix. This property
    ///     <b>MUST NOT</b> be defined when `perspective` is defined.
    /// </summary>
    [JsonPropertyName("orthographic")]
    public OrthographicCamera? Orthographic { get; set; }

    /// <summary>
    ///     A perspective camera containing properties to create a perspective projection matrix. This property
    ///     <b>MUST NOT</b> be defined when `orthographic` is defined.
    /// </summary>
    [JsonPropertyName("perspective")]
    public PerspectiveCamera? Perspective { get; set; }

    /// <summary>
    ///     Specifies if the camera uses a perspective or orthographic projection. Based on this, either the camera's
    ///     `perspective` or `orthographic` property <b>MUST</b> be defined.
    /// </summary>
    [JsonPropertyName("type")]
    public CameraType Type { get; set; }
}
