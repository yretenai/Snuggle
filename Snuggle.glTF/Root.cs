using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Snuggle.glTF;

/// <summary>The root object for a glTF asset.</summary>
public record Root : Property {
    /// <summary>Names of glTF extensions used in this asset.</summary>
    [JsonPropertyName("extensionsUsed")]
    public List<string>? ExtensionsUsed { get; set; }

    /// <summary>Names of glTF extensions required to properly load this asset.</summary>
    [JsonPropertyName("extensionsRequired")]
    public List<string>? ExtensionsRequired { get; set; }

    /// <summary>An array of accessors. An accessor is a typed view into a bufferView.</summary>
    [JsonPropertyName("accessors")]
    public List<Accessor>? Accessors { get; set; }

    /// <summary>An array of keyframe animations.</summary>
    [JsonPropertyName("animations")]
    public List<Animation>? Animations { get; set; }

    /// <summary>Metadata about the glTF asset.</summary>
    [JsonPropertyName("asset")]
    public Asset Asset { get; set; } = new();

    /// <summary>An array of buffers. A buffer points to binary geometry, animation, or skins.</summary>
    [JsonPropertyName("buffers")]
    public List<Buffer>? Buffers { get; set; }

    /// <summary>An array of bufferViews. A bufferView is a view into a buffer generally representing a subset of the buffer.</summary>
    [JsonPropertyName("bufferViews")]
    public List<BufferView>? BufferViews { get; set; }

    /// <summary>An array of cameras. A camera defines a projection matrix.</summary>
    [JsonPropertyName("cameras")]
    public List<Camera>? Cameras { get; set; }

    /// <summary>An array of images. An image defines data used to create a texture.</summary>
    [JsonPropertyName("images")]
    public List<Image>? Images { get; set; }

    /// <summary>An array of materials. A material defines the appearance of a primitive.</summary>
    [JsonPropertyName("materials")]
    public List<Material>? Materials { get; set; }

    /// <summary>An array of meshes. A mesh is a set of primitives to be rendered.</summary>
    [JsonPropertyName("meshes")]
    public List<Mesh>? Meshes { get; set; }

    /// <summary>An array of nodes.</summary>
    [JsonPropertyName("nodes")]
    public List<Node>? Nodes { get; set; }

    /// <summary>An array of samplers. A sampler contains properties for texture filtering and wrapping modes.</summary>
    [JsonPropertyName("samplers")]
    public List<Sampler>? Samplers { get; set; }

    /// <summary>The index of the default scene. This property <b>MUST NOT</b> be defined, when `scenes` is undefined.</summary>
    [JsonPropertyName("scene")]
    public int? Scene { get; set; }

    /// <summary>An array of scenes.</summary>
    [JsonPropertyName("scenes")]
    public List<Scene>? Scenes { get; set; }

    /// <summary>An array of skins. A skin is defined by joints and matrices.</summary>
    [JsonPropertyName("skins")]
    public List<Skin>? Skins { get; set; }

    /// <summary>An array of textures.</summary>
    [JsonPropertyName("textures")]
    public List<Texture>? Textures { get; set; }

    public (Mesh Mesh, int Id) CreateMesh() {
        Meshes ??= new List<Mesh>();
        var id = Meshes.Count;
        var mesh = new Mesh();
        Meshes.Add(mesh);
        return (mesh, id);
    }

    public (Accessor Accessor, int Id) BuildAccessor<T>(T[] array, Stream buffer, BufferViewTarget target, AccessorType type, AccessorComponentType componentType) where T : struct => BuildAccessor(BuildBufferView(MemoryMarshal.AsBytes(array.AsSpan()), buffer, Unsafe.SizeOf<T>(), target).Id, array.Length, 0, type, componentType);

    public (Accessor Accessor, int Id) BuildAccessor(int bufferView, int count, int offset, AccessorType type, AccessorComponentType componentType) {
        Accessors ??= new List<Accessor>();
        var id = Accessors.Count;
        var accessor = new Accessor {
            BufferView = bufferView,
            ByteOffset = offset,
            Count = count,
            Type = type,
            ComponentType = componentType,
        };
        Accessors.Add(accessor);
        return (accessor, id);
    }

    public (BufferView View, int Id) BuildBufferView(Span<byte> data, Stream buffer, int stride, BufferViewTarget target) {
        BufferViews ??= new List<BufferView>();
        var id = BufferViews.Count;
        var lastBufferView = BufferViews.LastOrDefault();
        var offset = 0;
        if (lastBufferView != null) {
            offset = lastBufferView.ByteOffset + lastBufferView.ByteLength;
        }

        var bufferView = new BufferView {
            ByteLength = data.Length,
            ByteOffset = offset,
            Buffer = 0,
            ByteStride = target == BufferViewTarget.ArrayBuffer ? stride : null,
            Target = target,
        };
        BufferViews.Add(bufferView);
        buffer.Write(data);
        return (bufferView, id);
    }

    public (Texture Texture, int Id) CreateTexture(string path, WrapMode wrapX, WrapMode wrapY, MagnificationFilter? mag, MinificationFilter? min) {
        Textures ??= new List<Texture>();
        var id = Textures.Count;
        var texture = new Texture {
            Source = CreateImage(path).Id,
            Sampler = CreateSampler(mag, min, wrapX, wrapY).Id,
        };
        Textures.Add(texture);
        return (texture, id);
        
    }

    private (Sampler Sampler, int Id) CreateSampler(MagnificationFilter? mag, MinificationFilter? min, WrapMode wrapU, WrapMode wrapV) {
        Samplers ??= new List<Sampler>();
        var id = Samplers.Count;
        var sampler = new Sampler {
            MinificationFilter = min,
            MagnificationFilter = mag,
            WrapS = wrapU,
            WrapT = wrapV,
        };
        Samplers.Add(sampler);
        return (sampler, id);
    }

    private (Image Source, int Id) CreateImage(string path) {
        Images ??= new List<Image>();
        var id = Images.Count;
        var image = new Image {
            Uri = path,
        };
        Images.Add(image);
        return (image, id);
    }

    public (Material Material, int Id) CreateMaterial(Root gltf) {
        Materials ??= new List<Material>();
        var id = Materials.Count;
        var material = new Material();
        Materials.Add(material);
        return (material, id);
    }
}
