using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DragonLib;
using Snuggle.Core;
using Snuggle.Core.Implementations;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Options;
using Snuggle.glTF;
using Buffer = Snuggle.glTF.Buffer;
using Material = Snuggle.Core.Implementations.Material;
using Mesh = Snuggle.Core.Implementations.Mesh;

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
        if (GltfExists(path)) {
            return;
        }

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

    private static bool GltfExists(string path) {
        path = Path.GetFullPath(path);
        return File.Exists(path) || File.Exists(Path.Combine(Path.ChangeExtension(path, null), Path.GetFileName(path)));
    }

    public static void Save(GameObject gameObject, string path, ObjectDeserializationOptions deserializationOptions, SnuggleExportOptions exportOptions, SnuggleMeshExportOptions options) {
        if (GltfExists(path)) {
            return;
        }

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

        foreach (var (meshNode, skinnedMeshRenderer) in skinnedMeshes) {
            var meshElement = gltf.Meshes![meshNode.Mesh!.Value];
            if (!meshElement.Primitives.Any(x => x.Attributes.ContainsKey("JOINTS_0"))) {
                continue;
            }

            var hashes = new Dictionary<uint, (long, string)>(skinnedMeshRenderer.Bones.Count);
            var isValid = true;
            CreateBoneHash(gameObject, nodeTree, hashes, gameObject);
            foreach (var t in skinnedMeshRenderer.Bones) {
                if (!CreateBoneHash(t.Value?.GameObject.Value, nodeTree, hashes, gameObject)) {
                    isValid = false;
                    break;
                }
            }

            if (!isValid) {
                break;
            }

            var matrices = new List<Matrix4x4>();
            var skinnedMesh = skinnedMeshRenderer.Mesh.Value!;
            var skin = gltf.CreateSkin();
            if (skinnedMesh.BoneNameHashes.Where((t, i) => !CreateBoneJoint(hashes, t, nodeTree, skinnedMesh.BindPose!.Value.Span[i].GetNumerics(), matrices, skin, options)).Any()) {
                isValid = false;
            }

            if (!isValid) {
                break;
            }

            skin.Skin.InverseBindMatrices = gltf.CreateAccessor(matrices.ToArray(), buffer, null, AccessorType.MAT4, AccessorComponentType.Float, 0).Id;

            meshNode.Skin = skin.Id;
        }

        SaveGltf(path, gltf, buffer, existingMaterials.Count > 0);
    }

    private static bool CreateBoneHash(GameObject? boneGameObject, IReadOnlyDictionary<(long, string), (Node Node, int Id)> nodeTree, IDictionary<uint, (long, string)> hashes, GameObject root) {
        if (boneGameObject == null) {
            return false;
        }

        var composite = boneGameObject.GetCompositeId();

        if (!nodeTree.ContainsKey(composite)) {
            return false;
        }

        var name = GetTransformPath(boneGameObject, root);
        hashes[CRC.GetDigest(name)] = composite;
        int index;
        while ((index = name.IndexOf("/", StringComparison.Ordinal)) >= 0) {
            name = name[(index + 1)..];
            hashes[CRC.GetDigest(name)] = composite;
        }

        return true;
    }

    private static bool CreateBoneJoint(IReadOnlyDictionary<uint, (long, string)> hashes, uint id, IReadOnlyDictionary<(long, string), (Node Node, int Id)> nodeTree, Matrix4x4 matrix, ICollection<Matrix4x4> matrices, (Skin Skin, int Id) skin, SnuggleMeshExportOptions options) {
        if (!hashes.TryGetValue(id, out var boneCompositeId) || !nodeTree.TryGetValue(boneCompositeId, out var boneId)) {
            return false;
        }

        if (options.MirrorXPosition) {
            var mirror = Matrix4x4.CreateScale(-1, 1, 1);
            matrices.Add(mirror * matrix * mirror);
        } else {
            matrices.Add(matrix);
        }

        skin.Skin.Joints.Add(boneId.Id);
        return true;
    }

    public static string GetTransformPath(GameObject? gameObject, GameObject? rootObject) {
        if (gameObject == null) {
            throw new InvalidOperationException();
        }

        var name = gameObject.Name;

        if ((rootObject == null || gameObject.GetCompositeId() != rootObject.GetCompositeId()) && gameObject.Parent.Value != null) {
            return GetTransformPath(gameObject.Parent.Value, rootObject) + "/" + name;
        }

        return name;
    }

    private static void CreateNodeTree(
        GameObject gameObject,
        Root gltf,
        Node node,
        Stream buffer,
        IDictionary<(long, string), (Node Node, int Id)> nodeTree,
        IDictionary<(long, string), int> existingMaterials,
        ICollection<(Node MeshNode, SkinnedMeshRenderer Data)> skinnedMeshes,
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

        if (options.MirrorXPosition) {
            var mirror = Matrix4x4.CreateScale(-1, 1, 1);
            var matrix = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(translation);
            matrix = mirror * matrix * mirror; // flip
            Matrix4x4.Decompose(matrix, out scale, out rotation, out translation);
        }
        
        node.Rotation = new List<double> { rotation.X, rotation.Y, rotation.Z, rotation.W };
        node.Scale = new List<double> { scale.X, scale.Y, scale.Z };
        node.Translation = new List<double> { translation.X, translation.Y, translation.Z };
        
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
                nodeTree[child.Value.GetCompositeId()] = (childNode, childId);

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
        IList<Material?>? materials,
        IDictionary<(long, string), int>? existingMaterials,
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

        var vertexStream = MeshConverter.GetVBO(mesh, out var vertexCount, out var descriptors, out var strides);
        var indexStream = MeshConverter.GetIBO(mesh);
        var indexSemantic = mesh.IndexFormat == IndexFormat.UInt16 ? AccessorComponentType.UnsignedShort : AccessorComponentType.UnsignedInt;

        var positions = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        var tangents = new Vector4[vertexCount];
        var color = new Vector4[vertexCount];
        var uv = new[] { new Vector2[vertexCount], new Vector2[vertexCount], new Vector2[vertexCount], new Vector2[vertexCount], new Vector2[vertexCount], new Vector2[vertexCount], new Vector2[vertexCount], new Vector2[vertexCount] };
        var joints = new ushort[vertexCount][];
        var weights = new Vector4[vertexCount];

        var hasNormals = false;
        var hasTangents = false;
        var hasColor = false;
        var hasUV = new[] { false, false, false, false, false, false, false, false };
        var hasJoints = false;
        var hasWeights = false;
        var minPos = new Vector3(float.MaxValue);
        var maxPos = new Vector3(float.MinValue);

        for (var i = 0; i < vertexCount; ++i) {
            var weightsTemp = new float[4];
            var jointsTemp = new int[4];
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
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (channel) {
                    case VertexChannel.Vertex:
                        positions[i] = new Vector3(floatValues.Take(3).ToArray());
                        if (options.MirrorXPosition) {
                            positions[i].X *= -1;
                        }

                        minPos = Vector3.Min(minPos, positions[i]);
                        maxPos = Vector3.Max(maxPos, positions[i]);
                        break;
                    case VertexChannel.Normal:
                        normals[i] = new Vector3(floatValues.Take(3).ToArray());
                        if (options.MirrorXNormal) {
                            normals[i].X *= -1;
                        }

                        hasNormals = true;
                        break;
                    case VertexChannel.Tangent:
                        tangents[i] = new Vector4(floatValues.Take(4).ToArray());
                        if (options.MirrorXTangent) {
                            tangents[i].X *= -1;
                        }

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
                        weightsTemp = floatValues.Take(4).ToArray();
                        hasWeights = true;
                        break;
                    case VertexChannel.SkinBoneIndex:
                        jointsTemp = uintValues.Take(4).ToArray();
                        hasJoints = true;
                        break;
                }
            }

            if ((!hasJoints || !hasWeights) && mesh.Skin?.Count > 0) {
                jointsTemp = mesh.Skin[i].Indices;
                weightsTemp = mesh.Skin[i].Weights;
                hasWeights = hasJoints = true;
            }

            hasWeights = hasJoints = hasWeights && hasJoints;

            // attempt to fix weights.
            if (hasWeights) {
                var ordered = jointsTemp.Zip(weightsTemp).OrderByDescending(x => x.Second).ToArray();
                joints[i] = ordered.Select(x => (ushort) x.First).ToArray();
                weights[i] = new Vector4(ordered.Select(x => x.Second).ToArray());
                var w = Vector4.Dot(weights[i], Vector4.One);
                if (w != 0.0f && w <= 1.0f) {
                    weights[i] /= w;
                }
            }
        }

        var accessors = new Dictionary<VertexChannel, int>();
        {
            var (accessor, accessorId) = gltf.CreateAccessor(positions, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC3, AccessorComponentType.Float);
            accessor.Min = new List<double> { minPos.X, minPos.Y, minPos.Z };
            accessor.Max = new List<double> { maxPos.X, maxPos.Y, maxPos.Z };
            accessors[VertexChannel.Vertex] = accessorId;
        }

        if (hasNormals) {
            accessors[VertexChannel.Normal] = gltf.CreateAccessor(normals, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC3, AccessorComponentType.Float).Id;
        }

        if (hasTangents) {
            accessors[VertexChannel.Tangent] = gltf.CreateAccessor(tangents, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC4, AccessorComponentType.Float).Id;
        }

        if (hasColor) {
            accessors[VertexChannel.Color] = gltf.CreateAccessor(color, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC3, AccessorComponentType.Float).Id;
        }

        for (var i = 0; i < 8; ++i) {
            if (!hasUV[i]) {
                continue;
            }

            accessors[VertexChannel.UV0 + i] = gltf.CreateAccessor(uv[i], buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC2, AccessorComponentType.Float).Id;
        }

        if (hasJoints && hasWeights) {
            accessors[VertexChannel.SkinBoneIndex] = gltf.CreateAccessor(
                    joints,
                    4,
                    buffer,
                    BufferViewTarget.ArrayBuffer,
                    AccessorType.VEC4,
                    AccessorComponentType.UnsignedShort,
                    8)
                .Id;
            accessors[VertexChannel.SkinWeight] = gltf.CreateAccessor(weights, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC4, AccessorComponentType.Float).Id;
        }

        if (indexSemantic == AccessorComponentType.UnsignedInt) {
            ReverseIndices<uint>(indexStream);
        } else {
            ReverseIndices<ushort>(indexStream);
        }

        var indexBuffer = gltf.CreateBufferView(indexStream.Span, buffer, -1, BufferViewTarget.ElementArrayBuffer).Id;

        for (var submeshIndex = 0; submeshIndex < mesh.Submeshes.Count; submeshIndex++) {
            var submesh = mesh.Submeshes[submeshIndex];
            var material = materials?.ElementAtOrDefault(submeshIndex);
            if (submesh.Topology != GfxPrimitiveType.Triangles) {
                continue;
            }

            var primitive = new Primitive { Mode = PrimitiveMode.Triangles };

            foreach (var (channel, index) in accessors) {
                primitive.Attributes[ChannelToSemantic[channel]] = index;
            }

            primitive.Indices = gltf.CreateAccessor(indexBuffer, (int) submesh.IndexCount, (int) submesh.FirstByte, AccessorType.SCALAR, indexSemantic).Id;
            if (material != null && existingMaterials!.TryGetValue(material.GetCompositeId(), out var materialId)) {
                primitive.Material = materialId;
            }

            element.Primitives.Add(primitive);
        }

        if (options.WriteMorphs && mesh.BlendShapeData.Channels.Count > 0) {
            var morphNames = new List<string>();
            var targets = new List<Dictionary<string, int>>();
            element.Weights = new List<double>();
            foreach (var channel in mesh.BlendShapeData.Channels) {
                morphNames.Add(channel.Name);
                var morphPositions = new Vector3[positions.Length];
                var morphNormals = new Vector3[normals.Length];
                var morphTangents = new Vector3[tangents.Length];
                var hasMorphNormals = false;
                var hasMorphTangents = false;
                minPos = new Vector3(float.MaxValue);
                maxPos = new Vector3(float.MinValue);
                for (var frameIndex = 0; frameIndex < channel.Count; frameIndex++) {
                    var fullIndex = channel.Index + frameIndex;
                    var shape = mesh.BlendShapeData.Shapes[fullIndex];

                    for (var vertexIndex = 0; vertexIndex < shape.VertexCount; vertexIndex++) {
                        var vertex = mesh.BlendShapeData.Vertices![(int) (shape.FirstVertex + vertexIndex)];

                        morphPositions[vertex.Index] = new Vector3(vertex.Vertex.X, vertex.Vertex.Y, vertex.Vertex.Z);
                        if (options.MirrorXPosition) {
                            morphPositions[vertex.Index].X *= -1;
                        }
                        minPos = Vector3.Min(minPos, morphPositions[vertex.Index]);
                        maxPos = Vector3.Max(maxPos, morphPositions[vertex.Index]);

                        if (shape.HasNormals && hasNormals) {
                            morphNormals[vertex.Index] = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                            if (options.MirrorXNormal) {
                                morphNormals[vertex.Index].X *= -1;
                            }
                            hasMorphNormals = true;
                        }

                        if (shape.HasTangents && hasTangents) {
                            morphTangents[vertex.Index] = new Vector3(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z);
                            if (options.MirrorXTangent) {
                                morphTangents[vertex.Index].X *= -1;
                            }
                            hasMorphTangents = false;
                        }
                    }
                }

                var (morphAccessor, morphAccessorId) = gltf.CreateAccessor(morphPositions, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC3, AccessorComponentType.Float);
                morphAccessor.Min = new List<double> { minPos.X, minPos.Y, minPos.Z };
                morphAccessor.Max = new List<double> { maxPos.X, maxPos.Y, maxPos.Z };
                var morph = new Dictionary<string, int> { ["POSITION"] = morphAccessorId };
                if (hasMorphNormals) {
                    morph["NORMAL"] = gltf.CreateAccessor(morphNormals, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC3, AccessorComponentType.Float).Id;
                }

                if (hasMorphTangents) {
                    morph["TANGENT"] = gltf.CreateAccessor(morphTangents, buffer, BufferViewTarget.ArrayBuffer, AccessorType.VEC3, AccessorComponentType.Float).Id;
                }

                targets.Add(morph);
                element.Weights.Add(0); // mesh.BlendShapeData.Weights[blendIndex] / 100
            }

            foreach (var primitive in element.Primitives) {
                primitive.Targets = targets;
            }

            if (morphNames.Count > 0) {
                element.Extras = new Dictionary<string, JsonValue> { { "targetNames", JsonValue.Create(morphNames)! } };
            }
        }
    }

    private static void CreateMaterial(Material material, IDictionary<(long, string), int> existingMaterials, Root gltf, string path, ObjectDeserializationOptions deserializationOptions, SnuggleExportOptions exportOptions) {
        if (existingMaterials.ContainsKey(material.GetCompositeId())) {
            return;
        }

        if (path.EndsWith(".gltf")) {
            path = Path.ChangeExtension(path, null);
        }

        SnuggleMaterialFile.Save(material, path, true);

        var mainTexId = -1;
        var normalTexId = -1;
        var (materialElement, materialId) = gltf.CreateMaterial();
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
}
