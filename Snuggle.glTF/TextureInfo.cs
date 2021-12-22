using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
/// Reference to a texture.
/// </summary>
public record TextureInfo : Property {
    /// <summary>
    /// The index of the texture.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// This integer value is used to construct a string in the format `TEXCOORD_&lt;set index&gt;` which is a reference to a key in `mesh.primitives.attributes` (e.g. a value of `0` corresponds to `TEXCOORD_0`).
    /// A mesh primitive <b>MUST</b> have the corresponding texture coordinate attributes for the material to be applicable to it.
    /// </summary>
    [JsonPropertyName("texCoord")]
    public int? TexCoord { get; set; } = 0;
}
