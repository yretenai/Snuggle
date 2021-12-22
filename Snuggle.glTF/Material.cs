using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
/// The material appearance of a primitive.
/// </summary>
public record Material : ChildOfRootProperty {
    /// <summary>
    /// A set of parameter values that are used to define the metallic-roughness material model from Physically Based Rendering (PBR) methodology.
    /// When undefined, all the default values of `pbrMetallicRoughness` <b>MUST</b> apply.
    /// </summary>
    [JsonPropertyName("pbrMetallicRoughness")]
    public PBRMaterial? PBR { get; set; }

    /// <summary>
    /// The tangent space normal texture. The texture encodes RGB components with linear transfer function.
    /// Each texel represents the XYZ components of a normal vector in tangent space.
    /// The normal vectors use the convention +X is right and +Y is up. +Z points toward the viewer.
    /// If a fourth component (A) is present, it <b>MUST</b> be ignored.
    /// When undefined, the material does not have a tangent space normal texture.
    /// </summary>
    [JsonPropertyName("normalTexture")]
    public NormalTextureInfo? NormalTexture { get; set; }

    /// <summary>
    /// The occlusion texture. The occlusion values are linearly sampled from the R channel.
    /// Higher values indicate areas that receive full indirect lighting and lower values indicate no indirect lighting.
    /// If other channels are present (GBA), they <b>MUST</b> be ignored for occlusion calculations.
    /// When undefined, the material does not have an occlusion texture.
    /// </summary>
    [JsonPropertyName("occlusionTexture")]
    public OcclusionTextureInfo? OcclusionTexture { get; set; }

    /// <summary>
    /// The emissive texture.
    /// It controls the color and intensity of the light being emitted by the material.
    /// This texture contains RGB components encoded with the sRGB transfer function.
    /// If a fourth component (A) is present, it <b>MUST</b> be ignored.
    /// When undefined, the texture <b>MUST</b> be sampled as having `1.0` in RGB components.
    /// </summary>
    [JsonPropertyName("emissiveTexture")]
    public TextureInfo? EmissiveTexture { get; set; }

    /// <summary>
    /// The factors for the emissive color of the material.
    /// This value defines linear multipliers for the sampled texels of the emissive texture.
    /// </summary>
    [JsonPropertyName("emissiveFactor")]
    public List<double>? EmissiveFactor { get; set; }

    /// <summary>
    /// The material's alpha rendering mode enumeration specifying the interpretation of the alpha value of the base color.
    /// </summary>
    [JsonPropertyName("alphaMode")]
    public AlphaMode AlphaMode { get; set; }

    /// <summary>
    /// Specifies the cutoff threshold when in `MASK` alpha mode.
    /// If the alpha value is greater than or equal to this value then it is rendered as fully opaque, otherwise, it is rendered as fully transparent.
    /// A value greater than `1.0` will render the entire material as fully transparent.
    /// This value <b>MUST</b> be ignored for other alpha modes.
    /// When `alphaMode` is not defined, this value <b>MUST NOT</b> be defined.
    /// </summary>
    [JsonPropertyName("alphaCutoff")]
    public double? AlphaCutoff { get; set; }

    /// <summary>
    /// Specifies whether the material is double sided.
    /// When this value is false, back-face culling is enabled.
    /// When this value is true, back-face culling is disabled and double-sided lighting is enabled.
    /// The back-face <b>MUST</b> have its normals reversed before the lighting equation is evaluated.
    /// </summary>
    [JsonPropertyName("doubleSided")]
    public bool? DoubleSided { get; set; } = false;
}
