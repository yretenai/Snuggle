using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using DragonLib;
using Snuggle.Core.Implementations;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Options;
using Snuggle.glTF;
using Buffer = Snuggle.glTF.Buffer;
using Material = Snuggle.Core.Implementations.Material;
using Mesh = Snuggle.Core.Implementations.Mesh;
using Quaternion = System.Numerics.Quaternion;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Snuggle.Converters;

public static class SnuggleMeshFile {
    private static readonly IReadOnlyDictionary<VertexChannel, string> ChannelToSemantic = new Dictionary<VertexChannel, string> {
        { VertexChannel.Vertex, "POSITION" },
        { VertexChannel.Normal, "NORMAL" },
        { VertexChannel.Tangent, "TANGENT" },
        { VertexChannel.Color, "COLOR" },
        { VertexChannel.UV0, "TEXCOORD_0" },
        { VertexChannel.UV1, "TEXCOORD_1" },
        { VertexChannel.UV2, "TEXCOORD_2" },
        { VertexChannel.UV3, "TEXCOORD_3" },
        { VertexChannel.UV4, "TEXCOORD_4" },
        { VertexChannel.UV5, "TEXCOORD_5" },
        { VertexChannel.UV6, "TEXCOORD_6" },
        { VertexChannel.UV7, "TEXCOORD_7" },
        { VertexChannel.SkinBoneIndex, "JOINTS_0" },
        { VertexChannel.SkinWeight, "WEIGHTS_0" },
    };

    private static JsonSerializerOptions Options =>
        new() {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

    public static void Save(Mesh mesh, string path, ObjectDeserializationOptions deserializationOptions, SnuggleExportOptions exportOptions, SnuggleMeshExportOptions options) {
        var (gltf, scene, buffer) = CreateGltf(mesh.SerializedFile.Assets?.PlayerSettings);

        var (meshNode, _) = scene.CreateNode(gltf);
        var (meshElement, meshId) = gltf.CreateMesh();
        meshNode.Mesh = meshId;
        meshNode.Name = meshElement.Name = mesh.Name;

        CreateMeshGeometry(
            mesh,
            null,
            null,
            meshElement,
            gltf,
            buffer,
            path,
            deserializationOptions,
            exportOptions,
            options);

        SaveGltf(path, gltf, buffer, false);
    }

    public static void Save(GameObject gameObject, string path, ObjectDeserializationOptions deserializationOptions, SnuggleExportOptions exportOptions, SnuggleMeshExportOptions options) {
        var (gltf, scene, buffer) = CreateGltf(gameObject.SerializedFile.Assets?.PlayerSettings);

        var (rootNode, rootId) = scene.CreateNode(gltf);
        rootNode.Name = gameObject.Name;

        var nodeTree = new Dictionary<(long, string), (Node Node, int Id)>();
        var skinnedMeshes = new List<(Node MeshNode, SkinnedMeshRenderer Data)>();

        gameObject = FindTopGeometry(gameObject, options.FindGameObjectParents) ?? gameObject;

        nodeTree[gameObject.GetCompositeId()] = (rootNode, rootId);
        var existingMaterials = new Dictionary<(long, string), int>();
        CreateNodeTree(
            gameObject,
            gltf,
            rootNode,
            buffer,
            nodeTree,
            existingMaterials,
            skinnedMeshes,
            path,
            deserializationOptions,
            exportOptions,
            options,
            true);
        
        // TODO(naomi): export glTF Skins

        SaveGltf(path, gltf, buffer, existingMaterials.Count > 0);
    }

    private static void CreateNodeTree(
        GameObject gameObject,
        Root gltf,
        Node node,
        Stream buffer,
        Dictionary<(long, string), (Node Node, int Id)> nodeTree,
        Dictionary<(long, string), int> existingMaterials,
        List<(Node MeshNode, SkinnedMeshRenderer Data)> skinnedMeshes,
        string path,
        ObjectDeserializationOptions deserializationOptions,
        SnuggleExportOptions exportOptions,
        SnuggleMeshExportOptions options,
        bool buildMeshes) {
        var rotation = Quaternion.Identity;
        var translation = Vector3.Zero;
        var scale = Vector3.One;
        if (gameObject.FindComponent(UnityClassId.Transform).Value is Transform transform) {
            rotation = new Quaternion(transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z, transform.Rotation.W);
            translation = new Vector3(transform.Translation.X, transform.Translation.Y, transform.Translation.Z);
            scale = new Vector3(transform.Scale.X, transform.Scale.Y, transform.Scale.Z);
        }

        node.Name = gameObject.Name;

        var mirror = Matrix4x4.CreateScale(-1, 1, 1);
        var matrix = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(translation);
        matrix = mirror * matrix * mirror; // flip
        Matrix4x4.Decompose(matrix, out scale, out rotation, out translation);
        node.Rotation = new List<double> { rotation.X, rotation.Y, rotation.Z, rotation.W };
        node.Scale = new List<double> { scale.X, scale.Y, scale.Z };
        node.Translation = new List<double> { translation.X, translation.Y, translation.Y };

        if (buildMeshes) {
            if (gameObject.FindComponent(UnityClassId.MeshRenderer, UnityClassId.SkinnedMeshRenderer).Value is SkinnedMeshRenderer skinnedMeshRenderer && skinnedMeshRenderer.Mesh.Value != null) {
                skinnedMeshRenderer.Mesh.Value.Deserialize(ObjectDeserializationOptions.Default);
                var (meshElement, meshId) = gltf.CreateMesh();
                CreateMeshGeometry(
                    skinnedMeshRenderer.Mesh.Value,
                    skinnedMeshRenderer.Materials.Select(x => x.Value).ToList(),
                    existingMaterials,
                    meshElement,
                    gltf,
                    buffer,
                    path,
                    deserializationOptions,
                    exportOptions,
                    options);
                node.Mesh = meshId;
                skinnedMeshes.Add((node, skinnedMeshRenderer));
            }

            if (gameObject.FindComponent(UnityClassId.MeshFilter).Value is MeshFilter filter && filter.Mesh.Value != null) {
                var materials = default(List<Material?>);
                if (gameObject.FindComponent(UnityClassId.MeshRenderer).Value is MeshRenderer renderer) {
                    materials = renderer.Materials.Select(x => x.Value).ToList();
                }
                
                filter.Mesh.Value.Deserialize(ObjectDeserializationOptions.Default);
                var (meshElement, meshId) = gltf.CreateMesh();
                CreateMeshGeometry(
                    filter.Mesh.Value,
                    materials,
                    existingMaterials,
                    meshElement,
                    gltf,
                    buffer,
                    path,
                    deserializationOptions,
                    exportOptions,
                    options);
                node.Mesh = meshId;
            }
        }

        if (options.FindGameObjectDescendants) {
            foreach (var child in gameObject.Children) {
                if (child.Value == null) {
                    continue;
                }

                var (childNode, childId) = node.CreateNode(gltf);
                nodeTree[gameObject.GetCompositeId()] = (childNode, childId);

                CreateNodeTree(
                    child.Value,
                    gltf,
                    childNode,
                    buffer,
                    nodeTree,
                    existingMaterials,
                    skinnedMeshes,
                    path,
                    deserializationOptions,
                    exportOptions,
                    options,
                    buildMeshes);
            }
        }
    }

    public static GameObject? FindTopGeometry(GameObject? gameObject, bool bubbleUp) {
        while (true) {
            if (gameObject?.Parent.Value == null || !bubbleUp) {
                return gameObject;
            }

            gameObject = gameObject.Parent.Value;
        }
    }

    private static void CreateMeshGeometry(
        Mesh mesh,
        List<Material?>? materials,
        Dictionary<(long, string), int>? existingMaterials,
        glTF.Mesh element,
        Root gltf,
        Stream buffer,
        string path,
        ObjectDeserializationOptions deserializationOptions,
        SnuggleExportOptions exportOptions,
        SnuggleMeshExportOptions options) {
        if (materials?.Any() == true) {
            foreach (var material in materials) {
                if (material == null) {
                    continue;
                }
                CreateMaterial(material, existingMaterials!, gltf, path, deserializationOptions, exportOptions);
            }
        }


        var vertexStream = MeshConverter.GetVBO(mesh, out var descriptors, out var strides);
        var indexStream = MeshConverter.GetIBO(mesh);
        var indexSemantic = mesh.IndexFormat == IndexFormat.UInt16 ? AccessorComponentType.UnsignedShort : AccessorComponentType.UnsignedInt;

        var positions = new Vector3[mesh.VertexData.VertexCount];
        var normals = new Vector3[mesh.VertexData.VertexCount];
        var tangents = new Vector4[mesh.VertexData.VertexCount];
        var color = new Vector4[mesh.VertexData.VertexCount];
        var uv = new[] { new Vector2[mesh.VertexData.VertexCount], new Vector2[mesh.VertexData.VertexCount], new Vector2[mesh.VertexData.VertexCount], new Vector2[mesh.VertexData.VertexCount], new Vector2[mesh.VertexData.VertexCount], new Vector2[mesh.VertexData.VertexCount], new Vector2[mesh.VertexData.VertexCount], new Vector2[mesh.VertexData.VertexCount] };
        var joints = new Vector4I[mesh.VertexData.VertexCount];
        var weights = new Vector4[mesh.VertexData.VertexCount];

        var hasPositions = false;
        var hasNormals = false;
        var hasTangents = false;
        var hasColor = false;
        var hasUV = new[] { false, false, false, false, false, false, false, false };
        var hasJoints = false;
        var hasWeights = false;
        var minPos = new Vector3(float.MaxValue);
        var maxPos = new Vector3(float.MinValue);

        for (var i = 0; i < mesh.VertexData.VertexCount; ++i) {
            foreach (var (channel, info) in descriptors) {
                var stride = strides[info.Stream];
                var offset = i * stride;
                var data = vertexStream[info.Stream][(offset + info.Offset)..].Span;
                if (info.Dimension == VertexDimension.None) {
                    continue;
                }

                var value = info.Unpack(data);
                var floatValues = value.Select(x => (float) Convert.ChangeType(x, TypeCode.Single)).Concat(new float[4]);
                var uintValues = value.Select(x => (int) Convert.ChangeType(x, TypeCode.Int32)).Concat(new int[4]);
                switch (channel) {
                    case VertexChannel.Vertex:
                        positions[i] = new Vector3(floatValues.Take(3).ToArray());
                        positions[i].X *= -1;
                        hasPositions = true;
                        minPos = Vector3.Min(minPos, positions[i]);
                        maxPos = Vector3.Max(maxPos, positions[i]);
                        break;
                    case VertexChannel.Normal:
                        normals[i] = new Vector3(floatValues.Take(3).ToArray());
                        normals[i].X *= -1;
                        hasNormals = true;
                        break;
                    case VertexChannel.Tangent:
                        tangents[i] = new Vector4(floatValues.Take(4).ToArray());
                        tangents[i].X *= -1;
                        hasTangents = true;
                        break;
                    case VertexChannel.Color when options.WriteVertexColors:
                        color[i] = new Vector4(floatValues.Take(4).ToArray());
                        hasColor = true;
                        break;
                    case VertexChannel.UV0:
                    case VertexChannel.UV1:
                    case VertexChannel.UV2:
                    case VertexChannel.UV3:
                    case VertexChannel.UV4:
                    case VertexChannel.UV5:
                    case VertexChannel.UV6:
                    case VertexChannel.UV7:
                        uv[channel - VertexChannel.UV0][i] = new Vector2(floatValues.Take(2).ToArray());
                        hasUV[channel - VertexChannel.UV0] = true;
                        break;
                    case VertexChannel.SkinWeight:
                        weights[i] = new Vector4(floatValues.Take(4).ToArray());
                        hasWeights = true;
                        break;
                    case VertexChannel.SkinBoneIndex:
                        joints[i] = new Vector4I(uintValues.Take(4).ToArray());
                        hasJoints = true;
                        break;
                }
            }

            if ((!hasJoints || !hasWeights) && mesh.Skin?.Count > 0) {
                joints[i] = new Vector4I(mesh.Skin[i].Indices);
                weights[i] = new Vector4(mesh.Skin[i].Weights);
                hasWeights = hasJoints = true;
            }

            hasWeights = hasJoints = hasWeights && hasJoints;
        }

        var accessors = new Dictionary<VertexChannel, int>();
        if (hasPositions) {
            var (accessor, accessorId) = gltf.BuildAccessor(positions, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC3, AccessorComponentType.Float);
            accessor.Min = new List<double> { minPos.X, minPos.Y, minPos.Z };
            accessor.Max = new List<double> { maxPos.X, maxPos.Y, maxPos.Z };
            accessors[VertexChannel.Vertex] = accessorId;
        }

        if (hasNormals) {
            accessors[VertexChannel.Normal] = gltf.BuildAccessor(normals, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC3, AccessorComponentType.Float).Id;
        }

        if (hasTangents) {
            accessors[VertexChannel.Tangent] = gltf.BuildAccessor(tangents, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC4, AccessorComponentType.Float).Id;
        }

        if (hasColor) {
            accessors[VertexChannel.Color] = gltf.BuildAccessor(color, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC3, AccessorComponentType.Float).Id;
        }

        for (var i = 0; i < 8; ++i) {
            if (!hasUV[i]) {
                continue;
            }

            accessors[VertexChannel.UV0 + i] = gltf.BuildAccessor(uv[i], buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC2, AccessorComponentType.Float).Id;
        }

        if (hasJoints && hasWeights) {
            // joints have to match semantic of the indices.
            if (indexSemantic == AccessorComponentType.UnsignedShort) {
                accessors[VertexChannel.SkinBoneIndex] = gltf.BuildAccessor(joints.Select(x => new Vector4S(x)).ToArray(), buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC4, AccessorComponentType.UnsignedShort).Id;
            } else {
                accessors[VertexChannel.SkinBoneIndex] = gltf.BuildAccessor(joints, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC4, AccessorComponentType.UnsignedInt).Id;
            }

            accessors[VertexChannel.SkinWeight] = gltf.BuildAccessor(weights, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC4, AccessorComponentType.Float).Id;
        }

        
        if (indexSemantic == AccessorComponentType.UnsignedInt) {
            ReverseIndices<uint>(indexStream);
        } else {
            ReverseIndices<ushort>(indexStream);
        }

        var indexBuffer = gltf.BuildBufferView(indexStream.Span, buffer, -1, BufferViewTarget.ElementArrayBuffer).Id;

        for (var submeshIndex = 0; submeshIndex < mesh.Submeshes.Count; submeshIndex++) {
            var submesh = mesh.Submeshes[submeshIndex];
            var material = materials?.ElementAtOrDefault(submeshIndex);
            if (submesh.Topology != GfxPrimitiveType.Triangles) {
                continue;
            }
            
            var primitive = new Primitive {
                Mode = PrimitiveMode.Triangles,
            };

            foreach (var (channel, index) in accessors) {
                primitive.Attributes[ChannelToSemantic[channel]] = index;
            }

            primitive.Indices = gltf.BuildAccessor(indexBuffer, (int) submesh.IndexCount, (int) submesh.FirstByte, AccessorType.SCALAR, indexSemantic).Id;
            if (material != null && existingMaterials!.TryGetValue(material.GetCompositeId(), out var materialId)) {
                primitive.Material = materialId;
            }
            
            // TODO(naomi): export glTF Morphs 

            element.Primitives.Add(primitive);
        }
    }

    private static void CreateMaterial(Material material, Dictionary<(long, string), int> existingMaterials, Root gltf, string path, ObjectDeserializationOptions deserializationOptions, SnuggleExportOptions exportOptions) {
        if (existingMaterials.ContainsKey(material.GetCompositeId())) {
            return;
        }

        if (path.EndsWith(".gltf")) {
            path = Path.ChangeExtension(path, null);
        }

        SnuggleMaterialFile.Save(material, path, true);

        var mainTexId = -1;
        var normalTexId = -1;
        var (materialElement, materialId) = gltf.CreateMaterial(gltf);
        materialElement.Name = material.Name;
        existingMaterials[material.GetCompositeId()] = materialId;
        
        foreach (var (name, texEnv) in material.SavedProperties.Textures) {
            if (texEnv.Texture.Value is not Texture2D texture) {
                continue;
            }

            texture.Deserialize(deserializationOptions);

            if (!existingMaterials.TryGetValue(texture.GetCompositeId(), out var texId)) {
                var texPath = Path.GetFileName(PathFormatter.Format(exportOptions.DecidePathTemplate(texture), "png", texture)); 
                texPath = Path.GetFileName(SnuggleTextureFile.Save(texture, Path.Combine(path, texPath), exportOptions, false));
                texId = gltf.CreateTexture(texPath, WrapMode.Repeat, WrapMode.Repeat, null, null).Id;
                existingMaterials[texture.GetCompositeId()] = texId;
            }

            if (name == "_MainTex") {
                mainTexId = texId;
            } else if (name == "_BumpMap" || name.Contains("Normal")) {
                normalTexId = texId;
            }
        }

        if (mainTexId > -1) {
            materialElement.PBR ??= new PBRMaterial();
            materialElement.PBR.BaseColorTexture = new TextureInfo { TexCoord = 0, Index = mainTexId };
        }
        
        if (material.SavedProperties.Colors.TryGetValue("_Color", out var baseColor) || material.SavedProperties.Colors.TryGetValue("_BaseColor", out baseColor)) {
            materialElement.PBR ??= new PBRMaterial();
            materialElement.PBR.BaseColorFactor = new List<double> { baseColor.R, baseColor.G, baseColor.B, baseColor.A };
        }

        if (normalTexId > -1) {
            materialElement.NormalTexture = new NormalTextureInfo { TexCoord = 0, Index = normalTexId };
        }
    }

    private static void ReverseIndices<T>(Memory<byte> indexStream) where T : struct {
        var cast = MemoryMarshal.Cast<byte, T>(indexStream.Span);
        for (var i = 0; i < cast.Length; i += 3) {
            if (cast.Length - i < 3) {
                break;
            }
            (cast[i + 0], cast[i + 2]) = (cast[i + 2], cast[i + 0]);
        }
    }

    private static (Root Root, Scene Scene, MemoryStream Buffer) CreateGltf(PlayerSettings? playerSettings) {
        var scene = new Scene();
        var root = new Root { Scenes = new List<Scene> { scene }, Scene = 0, Asset = new Asset { Copyright = playerSettings?.CompanyName } };
        var buffer = new MemoryStream();
        return (root, scene, buffer);
    }

    private static void SaveGltf(string output, Root root, MemoryStream buffer, bool makeDirectory) {
        output = Path.GetFullPath(output);
        if (buffer.Length > 0 || makeDirectory) {
            output = Path.Combine(Path.ChangeExtension(output, null), Path.GetFileName(output));
        }
        output.EnsureDirectoryExists();
        
        if (buffer.Length > 0) {

            var binName = Path.GetFileNameWithoutExtension(output) + ".bin";
            root.Buffers = new List<Buffer> { new() { ByteLength = (int) buffer.Length, Uri = binName } };
            buffer.Position = 0;
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(output)!, binName), buffer.ToArray());
        }

        using var file = File.OpenWrite(output);
        JsonSerializer.Serialize(file, root, Options);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct Vector4I {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;
        public readonly int W;

        public Vector4I() {
            X = 0;
            Y = 0;
            Z = 0;
            W = 0;
        }

        public Vector4I(IReadOnlyList<int> array) {
            X = array[0];
            Y = array[1];
            W = array[2];
            Z = array[3];
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct Vector4S {
        public readonly ushort X;
        public readonly ushort Y;
        public readonly ushort Z;
        public readonly ushort W;

        public Vector4S(Vector4I v4i) {
            X = (ushort) v4i.X;
            Y = (ushort) v4i.Y;
            Z = (ushort) v4i.Z;
            W = (ushort) v4i.W;
        }
    }
}
