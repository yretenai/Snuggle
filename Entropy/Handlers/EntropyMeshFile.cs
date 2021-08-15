using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Equilibrium.Converters;
using Equilibrium.Implementations;
using Equilibrium.Models;
using Equilibrium.Models.Objects.Graphics;
using Equilibrium.Options;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using Material = Equilibrium.Implementations.Material;
using Mesh = Equilibrium.Implementations.Mesh;

namespace Entropy.Handlers {
    public static class EntropyMeshFile {
        public static void Save(Mesh mesh, string path) {
            var scene = new SceneBuilder();
            var meshNode = CreateMesh(mesh);
            scene.AddRigidMesh(meshNode, AffineTransform.Identity);
            var gltf = scene.ToGltf2();

            if (gltf.LogicalImages.Any()) {
                path = Path.Combine(path, Path.GetFileName(path));
            }

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) &&
                !Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            gltf.SaveGLTF(Path.ChangeExtension(path, ".gltf"), new WriteSettings { JsonIndented = true });
        }

        public static void Save(Component component, string path) {
            var gameObject = component.GameObject.Value;
            if (gameObject == null) {
                return;
            }

            Save(gameObject, EntropyFile.GetResultPath(path, gameObject));
        }

        public static GameObject? FindTopGeometry(GameObject? gameObject) {
            while (true) {
                if (gameObject?.FindComponent(UnityClassId.Transform).Value is not Transform transform) {
                    return null;
                }

                if (transform.Parent.Value?.GameObject.Value == null) {
                    return gameObject;
                }

                gameObject = transform.Parent.Value.GameObject.Value;
            }
        }

        public static void Save(GameObject gameObject, string path) {
            var scene = new SceneBuilder();
            gameObject = FindTopGeometry(gameObject) ?? gameObject;

            var node = new NodeBuilder();
            scene.AddNode(node);

            path = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path));

            var nodeTree = new Dictionary<long, NodeBuilder>();
            var skinnedMeshes = new List<(Mesh mesh, List<Material?>)>();
            var hashTree = new Dictionary<uint, NodeBuilder>();
            BuildGameObject(gameObject, scene, node, nodeTree, skinnedMeshes, path);
            BuildHashTree(nodeTree, hashTree, gameObject.FindComponent(UnityClassId.Transform).Value as Transform);
            foreach (var (skinnedMesh, materials) in skinnedMeshes) {
                var skin = new (NodeBuilder, Matrix4x4)[skinnedMesh.BoneNameHashes.Count];
                for (var i = 0; i < skin.Length; ++i) {
                    var hash = skinnedMesh.BoneNameHashes[i];
                    var hashNode = hashTree[hash];
                    var matrix = skinnedMesh.BindPose!.Value.Span[i].GetNumerics();
                    skin[i] = (hashNode, matrix);
                }

                scene.AddSkinnedMesh(CreateMesh(skinnedMesh, materials, path), skin);
            }

            var gltf = scene.ToGltf2();

            if (gltf.LogicalImages.Any()) {
                path = Path.Combine(path, Path.GetFileName(path));
            }

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) &&
                !Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            gltf.SaveGLTF(path + ".gltf", new WriteSettings { JsonIndented = true });
        }

        private static void BuildHashTree(IReadOnlyDictionary<long, NodeBuilder> nodeTree, IDictionary<uint, NodeBuilder> hashTree, Transform? transform) {
            if (transform == null) {
                return;
            }

            var name = GetTransformPath(transform);
            var crc = new CRC();
            var bytes = Encoding.UTF8.GetBytes(name);
            crc.Update(bytes, 0, (uint) bytes.Length);
            var node = nodeTree[transform.PathId];
            hashTree[crc.GetDigest()] = node;
            int index;
            while ((index = name.IndexOf("/", StringComparison.Ordinal)) >= 0) {
                name = name[(index + 1)..];
                crc = new CRC();
                bytes = Encoding.UTF8.GetBytes(name);
                crc.Update(bytes, 0, (uint) bytes.Length);
                hashTree[crc.GetDigest()] = node;
            }

            foreach (var child in transform.Children) {
                BuildHashTree(nodeTree, hashTree, child.Value);
            }
        }

        private static string GetTransformPath(Transform transform) {
            if (transform.GameObject.Value == null) {
                throw new InvalidOperationException();
            }

            var name = transform.GameObject.Value.Name;

            if (transform.Parent.Value != null) {
                return GetTransformPath(transform.Parent.Value) + "/" + name;
            }

            return name;
        }

        private static void BuildGameObject(GameObject gameObject, SceneBuilder scene, NodeBuilder node, Dictionary<long, NodeBuilder> nodeTree, ICollection<(Mesh, List<Material?>)> skinnedMeshes, string? path) {
            if (gameObject.FindComponent(UnityClassId.Transform).Value is not Transform transform) {
                return;
            }

            node.Name = gameObject.Name;
            nodeTree[transform.PathId] = node;

            var (rX, rY, rZ, rW) = transform.Rotation;
            var (tX, tY, tZ) = transform.Translation;
            var (sX, sY, sZ) = transform.Scale;
            node.LocalMatrix = Matrix4x4.CreateScale(sX, sY, sZ) * Matrix4x4.CreateFromQuaternion(new Quaternion(rX, rY, rZ, rW)) * Matrix4x4.CreateTranslation(new Vector3(tX, tY, tZ));

            var materials = new List<Material?>();
            if (gameObject.FindComponent(UnityClassId.MeshRenderer, UnityClassId.SkinnedMeshRenderer).Value is Renderer renderer) {
                materials = renderer.Materials.Select(x => x.Value).ToList();
                if (renderer is SkinnedMeshRenderer skinnedMeshRenderer &&
                    skinnedMeshRenderer.Mesh.Value != null) {
                    if (skinnedMeshRenderer.Mesh.Value.ShouldDeserialize) {
                        skinnedMeshRenderer.Mesh.Value.Deserialize(ObjectDeserializationOptions.Default);
                    }

                    skinnedMeshes.Add((skinnedMeshRenderer.Mesh.Value, materials));
                }
            }

            if (gameObject.FindComponent(UnityClassId.MeshFilter).Value is MeshFilter filter &&
                filter.Mesh.Value != null) {
                if (filter.Mesh.Value.ShouldDeserialize) {
                    filter.Mesh.Value.Deserialize(ObjectDeserializationOptions.Default);
                }

                scene.AddRigidMesh(CreateMesh(filter.Mesh.Value, materials, path), node);
            }

            foreach (var child in transform.Children) {
                if (child.Value?.GameObject.Value == null) {
                    continue;
                }

                var childNode = node.CreateNode();
                scene.AddNode(childNode);
                BuildGameObject(child.Value.GameObject.Value, scene, childNode, nodeTree, skinnedMeshes, path);
            }
        }

        private static IMeshBuilder<MaterialBuilder> CreateMesh(Mesh mesh, List<Material?>? materials = null, string? path = null) {
            var meshBuilder = new MeshBuilder<VertexPositionNormalTangent, VertexColor1Texture2, VertexJoints4>(mesh.Name);

            var vertexStream = MeshConverter.GetVBO(mesh, out var descriptors, out var strides);
            var indexStream = MeshConverter.GetIBO(mesh);
            var indices = mesh.IndexFormat == IndexFormat.UInt16 ? MemoryMarshal.Cast<byte, ushort>(indexStream.Span).ToArray().Select(x => (int) x).ToArray() : MemoryMarshal.Cast<byte, int>(indexStream.Span);

            for (var submeshIndex = 0; submeshIndex < mesh.Submeshes.Count; submeshIndex++) {
                var submesh = mesh.Submeshes[submeshIndex];
                var material = new MaterialBuilder($"Submesh{submeshIndex}");
                CreateMaterial(material, materials?.ElementAtOrDefault(submeshIndex), path);

                var primitive = meshBuilder.UsePrimitive(material);

                var indicesPerSurface = submesh.Topology switch {
                    GfxPrimitiveType.Points => 1,
                    GfxPrimitiveType.Lines => 2,
                    GfxPrimitiveType.Triangles => 3,
                    GfxPrimitiveType.Quads => 4,
                    GfxPrimitiveType.Strip => throw new NotSupportedException(),
                    GfxPrimitiveType.TriangleStrip => throw new NotSupportedException(),
                    _ => throw new NotSupportedException(),
                };

                var xyvnt = new VertexPositionNormalTangent[submesh.VertexCount];
                Array.Fill(xyvnt, new VertexPositionNormalTangent());
                var cuv = new VertexColor1Texture2[submesh.VertexCount];
                Array.Fill(cuv, new VertexColor1Texture2());
                var joint = new VertexJoints4[submesh.VertexCount];
                for (var i = 0; i < submesh.VertexCount; ++i) {
                    var joints = Span<int>.Empty;
                    var weights = Span<float>.Empty;

                    foreach (var (channel, info) in descriptors) {
                        var stride = strides[info.Stream];
                        var offset = (submesh.FirstVertex + i) * stride;
                        var data = vertexStream[info.Stream][(offset + info.Offset)..].Span;
                        if (info.Dimension == VertexDimension.None) {
                            continue;
                        }

                        var value = info.Unpack(ref data);
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
                            case VertexChannel.Color:
                                cuv[i].Color = new Vector4(floatValues.Take(4).ToArray());
                                break;
                            case VertexChannel.UV0:
                                cuv[i].TexCoord0 = new Vector2(floatValues.Take(2).ToArray());
                                break;
                            case VertexChannel.UV1:
                                cuv[i].TexCoord1 = new Vector2(floatValues.Take(2).ToArray());
                                break;
                            case VertexChannel.UV2:
                            case VertexChannel.UV3:
                            case VertexChannel.UV4:
                            case VertexChannel.UV5:
                            case VertexChannel.UV6:
                            case VertexChannel.UV7:
                                break;
                            case VertexChannel.SkinWeight:
                                weights = floatValues.Take(4).ToArray();
                                break;
                            case VertexChannel.SkinBoneIndex:
                                joints = uintValues.Take(4).ToArray();
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }

                    if (joints.IsEmpty &&
                        mesh.Skin.Count > 0) {
                        joints = mesh.Skin[submesh.FirstVertex + i].Indices;
                        weights = mesh.Skin[submesh.FirstVertex + i].Weights;
                    }

                    if (weights.IsEmpty) {
                        var fullWeights = new float[joints.Length];
                        Array.Fill(fullWeights, 1.0f);
                        weights = fullWeights;
                    }

                    joint[i] = new VertexJoints4(joints.ToArray().Zip(weights.ToArray()).ToArray());
                }

                var baseOffset = (int) (submesh.FirstByte / (mesh.IndexFormat == IndexFormat.UInt16 ? 2 : 4));
                var submeshIndices = indices.Slice(baseOffset, (int) submesh.IndexCount).ToArray();
                if (submesh.FirstByte > 0) {
                    var baseIndex = submeshIndices.Min();
                    for (var indiceIndex = 0; indiceIndex < submesh.IndexCount; ++indiceIndex) {
                        submeshIndices[indiceIndex] -= baseIndex;
                    }
                }

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

            return meshBuilder;
        }

        private static void CreateMaterial(MaterialBuilder materialBuilder, Material? material, string? path) {
            if (material == null ||
                path == null) {
                return;
            }

            materialBuilder.Name = material.Name;

            foreach (var (name, texEnv) in material.SavedProperties.Textures) {
                if (texEnv.Texture.Value is not Texture2D texture) {
                    continue;
                }

                if (texture.ShouldDeserialize) {
                    texture.Deserialize(EntropyCore.Instance.Settings.ObjectOptions);
                }

                var texPath = EntropyTextureFile.Save(texture, Path.Combine(path, texture.Name + ".bin"));

                if (name == "_MainTex") {
                    materialBuilder.WithBaseColor(texPath);
                } else if (name == "_BumpMap" ||
                           name.Contains("Normal")) {
                    materialBuilder.WithNormal(texPath);
                } else if (name.Contains("Spec")) {
                    materialBuilder.WithSpecularGlossiness(texPath);
                } else if (name.Contains("Metal")) {
                    materialBuilder.WithMetallicRoughness(texPath);
                } else if (name.Contains("Rough") ||
                           name.Contains("Smooth")) {
                    materialBuilder.WithMetallicRoughness(texPath);
                } else if (name.Contains("Emis")) {
                    materialBuilder.WithEmissive(texPath);
                }
            }
        }
    }
}
