using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>
/// The root object for a glTF asset.
/// </summary>
public record Root : Property {
    /// <summary>
    /// Names of glTF extensions used in this asset.
    /// </summary>
    [JsonPropertyName("extensionsUsed")]
    public List<string>? ExtensionsUsed { get; set; }

    /// <summary>
    /// Names of glTF extensions required to properly load this asset.
    /// </summary>
    [JsonPropertyName("extensionsRequired")]
    public List<string>? ExtensionsRequired { get; set; }

    /// <summary>
    /// An array of accessors.
    /// An accessor is a typed view into a bufferView.
    /// </summary>
    [JsonPropertyName("accessors")]
    public List<Accessor>? Accessors { get; set; }

    /// <summary>
    /// An array of keyframe animations.
    /// </summary>
    [JsonPropertyName("animations")]
    public List<Animation>? Animations { get; set; }

    /// <summary>
    /// Metadata about the glTF asset.
    /// </summary>
    [JsonPropertyName("asset")]
    public Asset Asset { get; set; } = new();

    /// <summary>
    /// An array of buffers.
    /// A buffer points to binary geometry, animation, or skins.
    /// </summary>
    [JsonPropertyName("buffers")]
    public List<Buffer>? Buffers { get; set; }

    /// <summary>
    /// An array of bufferViews.
    /// A bufferView is a view into a buffer generally representing a subset of the buffer.
    /// </summary>
    [JsonPropertyName("bufferViews")]
    public List<BufferView>? BufferViews { get; set; }

    /// <summary>
    /// An array of cameras.
    /// A camera defines a projection matrix.
    /// </summary>
    [JsonPropertyName("cameras")]
    public List<Camera>? Cameras { get; set; }

    /// <summary>
    /// An array of images.
    /// An image defines data used to create a texture.
    /// </summary>
    [JsonPropertyName("images")]
    public List<Image>? Images { get; set; }

    /// <summary>
    /// An array of materials.
    /// A material defines the appearance of a primitive.
    /// </summary>
    [JsonPropertyName("materials")]
    public List<Material>? Materials { get; set; }

    /// <summary>
    /// An array of meshes.
    /// A mesh is a set of primitives to be rendered.
    /// </summary>
    [JsonPropertyName("meshes")]
    public List<Mesh>? Meshes { get; set; }

    /// <summary>
    /// An array of nodes.
    /// </summary>
    [JsonPropertyName("nodes")]
    public List<Node>? Nodes { get; set; }

    /// <summary>
    /// An array of samplers.
    /// A sampler contains properties for texture filtering and wrapping modes.
    /// </summary>
    [JsonPropertyName("samplers")]
    public List<Sampler>? Samplers { get; set; }

    /// <summary>
    /// The index of the default scene.
    /// This property <b>MUST NOT</b> be defined, when `scenes` is undefined.
    /// </summary>
    [JsonPropertyName("scene")]
    public int? Scene { get; set; }

    /// <summary>
    /// An array of scenes.
    /// </summary>
    [JsonPropertyName("scenes")]
    public List<Scene>? Scenes { get; set; }

    /// <summary>
    /// An array of skins.
    /// A skin is defined by joints and matrices.
    /// </summary>
    [JsonPropertyName("skins")]
    public List<Skin>? Skins { get; set; }

    /// <summary>
    /// An array of textures.
    /// </summary>
    [JsonPropertyName("textures")]
    public List<Texture>? Textures { get; set; }
}
