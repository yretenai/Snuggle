using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.IO;
using SharpGLTF.Materials;
using SharpGLTF.Memory;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using SharpGLTF.Validation;
using Snuggle.Core.Implementations;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Options;
using Material = Snuggle.Core.Implementations.Material;
using Mesh = Snuggle.Core.Implementations.Mesh;

namespace Snuggle.Converters;

public static class SnuggleMeshFile {
    public static void Save(Mesh mesh, string path, ObjectDeserializationOptions deserializationOptions, SnuggleExportOptions exportOptions, SnuggleMeshExportOptions options) {
        var targetPath = Path.ChangeExtension(path, ".gltf");
        if (File.Exists(targetPath)) {
            return;
        }

        var scene = new SceneBuilder();
        var meshNode = CreateMesh(
            mesh,
            null,
            null,
            null,
            deserializationOptions,
            exportOptions,
            options);
        scene.AddRigidMesh(meshNode, AffineTransform.Identity);
        var gltf = scene.ToGltf2(SceneBuilderSchema2Settings.Default with { GeneratorName = "Snuggle" });

        if (gltf.LogicalImages.Any()) {
            path = Path.Combine(path, Path.GetFileName(path));
        }

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        gltf.SaveGLTF(targetPath, new WriteSettings { JsonIndented = true, ImageWriting = ResourceWriteMode.SatelliteFile, Validation = ValidationMode.TryFix, ImageWriteCallback = FixImage });
    }

    private static string FixImage(WriteContext context, string assetname, MemoryImage image) => Path.GetFileName(image.SourcePath);

    public static GameObject? FindTopGeometry(GameObject? gameObject, bool bubbleUp) {
        while (true) {
            if (gameObject?.Parent.Value == null || bubbleUp) {
                return gameObject;
            }

            gameObject = gameObject.Parent.Value;
        }
    }

    public static void Save(GameObject gameObject, string path, ObjectDeserializationOptions deserializationOptions, SnuggleExportOptions exportOptions, SnuggleMeshExportOptions options) {
        path = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path));

        if (File.Exists(path + ".gltf") || File.Exists(Path.Combine(path, Path.GetFileName(path)) + ".gltf")) {
            return;
        }

        var scene = new SceneBuilder();
        gameObject = FindTopGeometry(gameObject, options.FindGameObjectParents) ?? gameObject;

        var node = new NodeBuilder();
        scene.AddNode(node);

        var nodeTree = new Dictionary<(long, string), NodeBuilder>();
        var skinnedMeshes = new List<(Mesh mesh, List<Material?>)>();
        var hashTree = new Dictionary<uint, NodeBuilder>();
        BuildGameObject(
            gameObject,
            scene,
            node,
            nodeTree,
            skinnedMeshes,
            path,
            deserializationOptions,
            exportOptions,
            options);
        BuildHashTree(nodeTree, hashTree, gameObject);
        var saved = new Dictionary<(long, string), string>();
        foreach (var (skinnedMesh, materials) in skinnedMeshes) {
            var skin = new (NodeBuilder, Matrix4x4)[skinnedMesh.BoneNameHashes.Count];
            for (var i = 0; i < skin.Length; ++i) {
                var hash = skinnedMesh.BoneNameHashes[i];
                var hashNode = hashTree[hash];
                var matrix = skinnedMesh.BindPose!.Value.Span[i].GetNumerics();
                skin[i] = (hashNode, matrix);
            }

            scene.AddSkinnedMesh(
                CreateMesh(
                    skinnedMesh,
                    materials,
                    path,
                    saved,
                    deserializationOptions,
                    exportOptions,
                    options),
                skin);
        }

        var gltf = scene.ToGltf2(SceneBuilderSchema2Settings.Default with { GeneratorName = "Snuggle" });

        if (gltf.LogicalImages.Any()) {
            path = Path.Combine(path, Path.GetFileName(path));
        }

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        gltf.SaveGLTF(path + ".gltf", new WriteSettings { JsonIndented = true, ImageWriting = ResourceWriteMode.SatelliteFile, Validation = ValidationMode.TryFix, ImageWriteCallback = FixImage });
    }

    private static void BuildHashTree(IReadOnlyDictionary<(long, string), NodeBuilder> nodeTree, IDictionary<uint, NodeBuilder> hashTree, GameObject? gameObject) {
        if (gameObject == null) {
            return;
        }

        var name = GetTransformPath(gameObject);
        var crc = new CRC();
        var bytes = Encoding.UTF8.GetBytes(name);
        crc.Update(bytes, 0, (uint) bytes.Length);
        var node = nodeTree[gameObject.GetCompositeId()];
        hashTree[crc.GetDigest()] = node;
        int index;
        while ((index = name.IndexOf("/", StringComparison.Ordinal)) >= 0) {
            name = name[(index + 1)..];
            crc = new CRC();
            bytes = Encoding.UTF8.GetBytes(name);
            crc.Update(bytes, 0, (uint) bytes.Length);
            hashTree[crc.GetDigest()] = node;
        }

        foreach (var child in gameObject.Children) {
            BuildHashTree(nodeTree, hashTree, child.Value);
        }
    }

    private static string GetTransformPath(GameObject? gameObject) {
        if (gameObject == null) {
            throw new InvalidOperationException();
        }

        var name = gameObject.Name;

        if (gameObject.Parent.Value != null) {
            return GetTransformPath(gameObject.Parent.Value) + "/" + name;
        }

        return name;
    }

    private static void BuildGameObject(
        GameObject gameObject,
        SceneBuilder scene,
        NodeBuilder node,
        Dictionary<(long, string), NodeBuilder> nodeTree,
        ICollection<(Mesh, List<Material?>)> skinnedMeshes,
        string? path,
        ObjectDeserializationOptions deserializationOptions,
        SnuggleExportOptions exportOptions,
        SnuggleMeshExportOptions options) {
        if (gameObject.FindComponent(UnityClassId.Transform).Value is not Transform transform) {
            return;
        }

        node.Name = gameObject.Name;
        nodeTree[gameObject.GetCompositeId()] = node;

        var (rX, rY, rZ, rW) = transform.Rotation;
        var (tX, tY, tZ) = transform.Translation;
        var (sX, sY, sZ) = transform.Scale;
        node.LocalMatrix = Matrix4x4.CreateScale(sX, sY, sZ) * Matrix4x4.CreateFromQuaternion(new Quaternion(rX, rY, rZ, rW)) * Matrix4x4.CreateTranslation(new Vector3(tX, tY, tZ));

        var materials = new List<Material?>();
        if (gameObject.FindComponent(UnityClassId.MeshRenderer, UnityClassId.SkinnedMeshRenderer).Value is Renderer renderer) {
            materials = renderer.Materials.Select(x => x.Value).ToList();
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer && skinnedMeshRenderer.Mesh.Value != null) {
                skinnedMeshRenderer.Mesh.Value.Deserialize(ObjectDeserializationOptions.Default);
                skinnedMeshes.Add((skinnedMeshRenderer.Mesh.Value, materials));
            }
        }

        if (gameObject.FindComponent(UnityClassId.MeshFilter).Value is MeshFilter filter && filter.Mesh.Value != null) {
            filter.Mesh.Value.Deserialize(ObjectDeserializationOptions.Default);
            scene.AddRigidMesh(
                CreateMesh(
                    filter.Mesh.Value,
                    materials,
                    path,
                    null,
                    deserializationOptions,
                    exportOptions,
                    options),
                node);
        }

        if (options.FindGameObjectDescendants) {
            foreach (var child in gameObject.Children) {
                if (child.Value == null) {
                    continue;
                }

                var childNode = node.CreateNode();
                scene.AddNode(childNode);
                BuildGameObject(
                    child.Value,
                    scene,
                    childNode,
                    nodeTree,
                    skinnedMeshes,
                    path,
                    deserializationOptions,
                    exportOptions,
                    options);
            }
        }
    }

    private static IMeshBuilder<MaterialBuilder> CreateMesh(
        Mesh mesh,
        List<Material?>? materials,
        string? path,
        Dictionary<(long, string), string>? saved,
        ObjectDeserializationOptions deserializationOptions,
        SnuggleExportOptions exportOptions,
        SnuggleMeshExportOptions options) {
        var vertexStream = MeshConverter.GetVBO(mesh, out var descriptors, out var strides);
        var indexStream = MeshConverter.GetIBO(mesh);
        var indices = mesh.IndexFormat == IndexFormat.UInt16 ? MemoryMarshal.Cast<byte, ushort>(indexStream.Span).ToArray().Select(x => (int) x).ToArray() : MemoryMarshal.Cast<byte, int>(indexStream.Span);

        var xyvnt = new VertexPositionNormalTangent[mesh.VertexData.VertexCount];
        Array.Fill(xyvnt, new VertexPositionNormalTangent());
        var cuv = new VertexColor1Texture2[mesh.VertexData.VertexCount];
        Array.Fill(cuv, new VertexColor1Texture2());
        var joint = new VertexJoints4[mesh.VertexData.VertexCount];
        var hasWeights = false;
        for (var i = 0; i < mesh.VertexData.VertexCount; ++i) {
            var joints = Span<int>.Empty;
            var weights = Span<float>.Empty;

            xyvnt[i].Id = i;
            cuv[i].Id = i;
            cuv[i].Color = Vector4.One;
            joint[i].Id = i;

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
                        xyvnt[i].Position = new Vector3(floatValues.Take(3).ToArray());
                        break;
                    case VertexChannel.Normal:
                        xyvnt[i].Normal = new Vector3(floatValues.Take(3).ToArray());
                        break;
                    case VertexChannel.Tangent:
                        xyvnt[i].Tangent = new Vector4(floatValues.Take(4).ToArray());
                        break;
                    case VertexChannel.Color when options.WriteVertexColors:
                        cuv[i].Color = new Vector4(floatValues.Take(4).ToArray());
                        break;
                    case VertexChannel.UV0:
                        cuv[i].TexCoord0 = new Vector2(floatValues.Take(2).ToArray());
                        break;
                    case VertexChannel.UV1:
                        cuv[i].TexCoord1 = new Vector2(floatValues.Take(2).ToArray());
                        break;
                    case VertexChannel.SkinWeight:
                        weights = floatValues.Take(4).ToArray();
                        break;
                    case VertexChannel.SkinBoneIndex:
                        joints = uintValues.Take(4).ToArray();
                        break;
                }
            }

            if (joints.IsEmpty && mesh.Skin?.Count > 0) {
                joints = mesh.Skin[i].Indices;
                weights = mesh.Skin[i].Weights;
            }

            if (weights.IsEmpty) {
                var fullWeights = new float[joints.Length];
                Array.Fill(fullWeights, 1.0f);
                weights = fullWeights;
            }

            var jointsArray = joints.ToArray();
            var weightsArray = weights.ToArray();
            if (!hasWeights) {
                hasWeights = weightsArray.Any(x => x != 0);
            }

            joint[i] = new VertexJoints4(jointsArray.Zip(weightsArray).ToArray());
        }

        IMeshBuilder<MaterialBuilder> meshBuilder;
        if (hasWeights) {
            var meshBuilderAbs = new MeshBuilder<VertexPositionNormalTangent, VertexColor1Texture2, VertexJoints4>(mesh.Name);
            meshBuilder = meshBuilderAbs;
            for (var submeshIndex = 0; submeshIndex < mesh.Submeshes.Count; submeshIndex++) {
                var submesh = mesh.Submeshes[submeshIndex];
                var material = new MaterialBuilder($"Submesh{submeshIndex}");
                CreateMaterial(
                    material,
                    materials?.ElementAtOrDefault(submeshIndex),
                    path,
                    saved ?? new Dictionary<(long, string), string>(),
                    deserializationOptions,
                    exportOptions,
                    options);

                var primitive = meshBuilderAbs.UsePrimitive(material);

                var indicesPerSurface = submesh.Topology switch {
                    GfxPrimitiveType.Points => 1,
                    GfxPrimitiveType.Lines => 2,
                    GfxPrimitiveType.Triangles => 3,
                    GfxPrimitiveType.Quads => 4,
                    GfxPrimitiveType.Strip => throw new NotSupportedException(),
                    GfxPrimitiveType.TriangleStrip => throw new NotSupportedException(),
                    _ => throw new NotSupportedException(),
                };

                var baseOffset = (int) (submesh.FirstByte / (mesh.IndexFormat == IndexFormat.UInt16 ? 2 : 4));
                var submeshIndices = indices.Slice(baseOffset, (int) submesh.IndexCount).ToArray();

                for (var i = 0; i < submesh.IndexCount / indicesPerSurface; ++i) {
                    var index = submeshIndices.Skip(i * indicesPerSurface).Take(indicesPerSurface).Select(x => (xyvnt[x], cuv[x], joint[x])).ToArray();

                    switch (submesh.Topology) {
                        case GfxPrimitiveType.Triangles:
                            primitive.AddTriangle(index[0], index[1], index[2]);
                            break;
                        case GfxPrimitiveType.Quads:
                            primitive.AddQuadrangle(index[0], index[1], index[2], index[3]);
                            break;
                        case GfxPrimitiveType.Lines:
                            primitive.AddLine(index[0], index[1]);
                            break;
                        case GfxPrimitiveType.Points:
                            primitive.AddPoint(index[0]);
                            break;
                        case GfxPrimitiveType.TriangleStrip:
                        case GfxPrimitiveType.Strip:
                            throw new NotSupportedException();
                        default:
                            throw new NotSupportedException();
                    }
                }
            }
        } else {
            var meshBuilderAbs = new MeshBuilder<VertexPositionNormalTangent, VertexColor1Texture2>(mesh.Name);
            meshBuilder = meshBuilderAbs;
            for (var submeshIndex = 0; submeshIndex < mesh.Submeshes.Count; submeshIndex++) {
                var submesh = mesh.Submeshes[submeshIndex];
                var material = new MaterialBuilder($"Submesh{submeshIndex}");
                CreateMaterial(
                    material,
                    materials?.ElementAtOrDefault(submeshIndex),
                    path,
                    saved ?? new Dictionary<(long, string), string>(),
                    deserializationOptions,
                    exportOptions,
                    options);

                var primitive = meshBuilderAbs.UsePrimitive(material);

                var indicesPerSurface = submesh.Topology switch {
                    GfxPrimitiveType.Points => 1,
                    GfxPrimitiveType.Lines => 2,
                    GfxPrimitiveType.Triangles => 3,
                    GfxPrimitiveType.Quads => 4,
                    GfxPrimitiveType.Strip => throw new NotSupportedException(),
                    GfxPrimitiveType.TriangleStrip => throw new NotSupportedException(),
                    _ => throw new NotSupportedException(),
                };

                var baseOffset = (int) (submesh.FirstByte / (mesh.IndexFormat == IndexFormat.UInt16 ? 2 : 4));
                var submeshIndices = indices.Slice(baseOffset, (int) submesh.IndexCount).ToArray();

                for (var i = 0; i < submesh.IndexCount / indicesPerSurface; ++i) {
                    var index = submeshIndices.Skip(i * indicesPerSurface).Take(indicesPerSurface).Select(x => (xyvnt[x], cuv[x])).ToArray();

                    switch (submesh.Topology) {
                        case GfxPrimitiveType.Triangles:
                            primitive.AddTriangle(index[0], index[1], index[2]);
                            break;
                        case GfxPrimitiveType.Quads:
                            primitive.AddQuadrangle(index[0], index[1], index[2], index[3]);
                            break;
                        case GfxPrimitiveType.Lines:
                            primitive.AddLine(index[0], index[1]);
                            break;
                        case GfxPrimitiveType.Points:
                            primitive.AddPoint(index[0]);
                            break;
                        case GfxPrimitiveType.TriangleStrip:
                        case GfxPrimitiveType.Strip:
                            throw new NotSupportedException();
                        default:
                            throw new NotSupportedException();
                    }
                }
            }
        }

        if (options.WriteMorphs) {
            var morphNames = new List<string>();
            for (var blendIndex = 0; blendIndex < mesh.BlendShapeData.Channels.Count; blendIndex++) {
                var channel = mesh.BlendShapeData.Channels[blendIndex];
                var morph = meshBuilder.UseMorphTarget(blendIndex);
                morphNames.Add(channel.Name);
                for (var frameIndex = 0; frameIndex < channel.Count; frameIndex++) {
                    var fullIndex = channel.Index + frameIndex;
                    var shape = mesh.BlendShapeData.Shapes[fullIndex];

                    for (var vertexIndex = 0; vertexIndex < shape.VertexCount; vertexIndex++) {
                        var vertex = mesh.BlendShapeData.Vertices![(int) (shape.FirstVertex + vertexIndex)];

                        var geometryData = new VertexGeometryDelta { PositionDelta = new Vector3(vertex.Vertex.X, vertex.Vertex.Y, vertex.Vertex.Z) };

                        if (shape.HasNormals) {
                            geometryData.NormalDelta = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                        }

                        if (shape.HasTangents) {
                            geometryData.TangentDelta = new Vector3(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z);
                        }

                        morph.SetVertexDelta(xyvnt[vertex.Index], geometryData);
                    }
                }
            }

            if (morphNames.Count > 0) {
                meshBuilder.Extras = JsonContent.CreateFrom(new Dictionary<string, List<string>> { { "targetNames", morphNames } });
            }
        }

        return meshBuilder;
    }

    private static void CreateMaterial(
        MaterialBuilder materialBuilder,
        Material? material,
        string? path,
        Dictionary<(long, string), string> saved,
        ObjectDeserializationOptions deserializationOptions,
        SnuggleExportOptions exportOptions,
        SnuggleMeshExportOptions options) {
        if (material == null || path == null) {
            return;
        }

        materialBuilder.Name = material.Name;

        if (options.WriteTexture) {
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            foreach (var (name, texEnv) in material.SavedProperties.Textures) {
                if (texEnv.Texture.Value is not Texture2D texture) {
                    continue;
                }

                texture.Deserialize(deserializationOptions);

                if (!saved.TryGetValue(texture.GetCompositeId(), out var texPath)) {
                    texPath = SnuggleTextureFile.Save(texture, Path.Combine(path, texture.Name + "_" + texture.PathId + ".bin"), exportOptions, false);
                }

                if (name == "_MainTex") {
                    materialBuilder.WithBaseColor(new MemoryImage(texPath));
                } else if (name == "_BumpMap" || name.Contains("Normal")) {
                    materialBuilder.WithNormal(new MemoryImage(texPath));
                } else if (name.Contains("Spec")) {
                    materialBuilder.WithSpecularGlossiness(new MemoryImage(texPath));
                } else if (name.Contains("Metal")) {
                    materialBuilder.WithMetallicRoughness(new MemoryImage(texPath));
                } else if (name.Contains("Rough") || name.Contains("Smooth")) {
                    materialBuilder.WithMetallicRoughness(new MemoryImage(texPath));
                } else if (name.Contains("Emis")) {
                    materialBuilder.WithEmissive(new MemoryImage(texPath));
                }
            }
        }

        if (options.WriteMaterial) {
            SnuggleMaterialFile.SaveMaterial(material, path);
        }
    }
}
