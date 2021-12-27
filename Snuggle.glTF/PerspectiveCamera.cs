using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>A perspective camera containing properties to create a perspective projection matrix.</summary>
public record PerspectiveCamera : Property {
    /// <summary>
    ///     The floating-point aspect ratio of the field of view. When undefined, the aspect ratio of the rendering
    ///     viewport <b>MUST</b> be used.
    /// </summary>
    [JsonPropertyName("aspectRatio")]
    public double? AspectRatio { get; set; }

    /// <summary>The floating-point vertical field of view in radians. This value <b>SHOULD</b> be less than π.</summary>
    [JsonPropertyName("yfov")]
    public double FOV { get; set; }

    /// <summary>
    ///     The floating-point distance to the far clipping plane. When defined, `zfar` <b>MUST</b> be greater than
    ///     `znear`. If `zfar` is undefined, client implementations <b>SHOULD</b> use infinite projection matrix.
    /// </summary>
    [JsonPropertyName("zfar")]
    public double? FarPlane { get; set; }

    /// <summary>The floating-point distance to the near clipping plane.</summary>
    [JsonPropertyName("znear")]
    public double NearPlane { get; set; }
}
