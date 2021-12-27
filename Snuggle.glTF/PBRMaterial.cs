using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
///     A set of parameter values that are used to define the metallic-roughness material model from Physically-Based
///     Rendering (PBR) methodology.
/// </summary>
public record PBRMaterial : Property {
    /// <summary>
    ///     The factors for the base color of the material. This value defines linear multipliers for the sampled texels
    ///     of the base color texture.
    /// </summary>
    [JsonPropertyName("baseColorFactor")]
    public List<double>? BaseColorFactor { get; set; }

    /// <summary>
    ///     The base color texture. The first three components (RGB) <b>MUST</b> be encoded with the sRGB transfer
    ///     function. They specify the base color of the material. If the fourth component (A) is present, it represents the
    ///     linear alpha coverage of the material. Otherwise, the alpha coverage is equal to `1.0`. The `material.alphaMode`
    ///     property specifies how alpha is interpreted. The stored texels <b>MUST NOT</b> be premultiplied. When undefined,
    ///     the texture <b>MUST</b> be sampled as having `1.0` in all components.
    /// </summary>
    [JsonPropertyName("baseColorTexture")]
    public TextureInfo? BaseColorTexture { get; set; }

    /// <summary>
    ///     The factor for the metalness of the material. This value defines a linear multiplier for the sampled metalness
    ///     values of the metallic-roughness texture.
    /// </summary>
    [JsonPropertyName("metallicFactor")]
    public double? MetallicFactor { get; set; } = 1D;

    /// <summary>
    ///     The factor for the roughness of the material. This value defines a linear multiplier for the sampled roughness
    ///     values of the metallic-roughness texture.
    /// </summary>
    [JsonPropertyName("roughnessFactor")]
    public double? RoughnessFactor { get; set; } = 1D;

    /// <summary>
    ///     The metallic-roughness texture. The metalness values are sampled from the B channel. The roughness values are
    ///     sampled from the G channel. These values <b>MUST</b> be encoded with a linear transfer function. If other channels
    ///     are present (R or A), they <b>MUST</b> be ignored for metallic-roughness calculations. When undefined, the texture
    ///     <b>MUST</b> be sampled as having `1.0` in G and B components.
    /// </summary>
    [JsonPropertyName("metallicRoughnessTexture")]
    public TextureInfo? MetallicRoughnessTexture { get; set; }
}
