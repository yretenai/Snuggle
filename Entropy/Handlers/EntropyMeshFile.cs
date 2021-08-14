using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
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
            gltf.SaveGLB(Path.ChangeExtension(path, ".glb"), new WriteSettings { JsonIndented = true });
        }

        public static void Save(Component component, string path) {
            var gameObject = component.GameObject.Value;
            if (gameObject == null) {
                return;
            }

            Save(gameObject, path);
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
            BuildGameObject(gameObject, scene, node, Path.GetDirectoryName(path));
            var gltf = scene.ToGltf2();
            gltf.SaveGLB(Path.ChangeExtension(path, ".glb"), new WriteSettings { JsonIndented = true });
        }

        private static void BuildGameObject(GameObject gameObject, SceneBuilder scene, NodeBuilder node, string? path) {
            if (gameObject.FindComponent(UnityClassId.Transform).Value is not Transform transform) {
                return;
            }

            node.Name = gameObject.Name;

            var (rX, rY, rZ, rW) = transform.Rotation;
            var (sX, sY, sZ) = transform.Scale;
            var (tX, tY, tZ) = transform.Translation;

            var localMatrix = Matrix4x4.CreateScale(sX, sY, sZ) * Matrix4x4.CreateFromQuaternion(new Quaternion(rX, -rY, -rZ, rW)) * Matrix4x4.CreateTranslation(new Vector3(-tX, tY, tZ));
            node.LocalMatrix = localMatrix;

            var materials = new List<Material?>();
            if (gameObject.FindComponent(UnityClassId.MeshRenderer, UnityClassId.SkinnedMeshRenderer).Value is Renderer renderer) {
                materials = renderer.Materials.Select(x => x.Value).ToList();
                if (renderer is SkinnedMeshRenderer skinnedMeshRenderer &&
                    skinnedMeshRenderer.Mesh.Value != null) {
                    if (skinnedMeshRenderer.Mesh.Value.ShouldDeserialize) {
                        skinnedMeshRenderer.Mesh.Value.Deserialize(ObjectDeserializationOptions.Default);
                    }

                    scene.AddRigidMesh(CreateMesh(skinnedMeshRenderer.Mesh.Value, materials, path), node);
                    // TODO: BindPose and stuff I gues
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
                BuildGameObject(child.Value.GameObject.Value, scene, childNode, path);
            }
        }

        private static IMeshBuilder<MaterialBuilder> CreateMesh(Mesh mesh, List<Material?>? materials = null, string? path = null) {
            var meshBuilder = new MeshBuilder<VertexPositionNormalTangent, VertexColor1Texture2, VertexEmpty>(mesh.Name);

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
                for (var i = 0; i < submesh.VertexCount; ++i) {
                    foreach (var (channel, info) in descriptors) {
                        var stride = strides[info.Stream];
                        var offset = (submesh.FirstVertex + i) * stride;
                        var data = vertexStream[info.Stream][(offset + info.Offset)..].Span;
                        if (info.Dimension == VertexDimension.None) {
                            continue;
                        }

                        var value = info.Unpack(ref data);
                        var floatValues = value.Select(Convert.ToSingle).Concat(new float[4]);
                        switch (channel) {
                            case VertexChannel.Vertex:
                                xyvnt[i].Position = new Vector3(floatValues.ToArray());
                                break;
                            case VertexChannel.Normal:
                                xyvnt[i].Normal = new Vector3(floatValues.ToArray());
                                break;
                            case VertexChannel.Tangent:
                                xyvnt[i].Tangent = new Vector4(floatValues.ToArray());
                                break;
                            case VertexChannel.Color:
                                cuv[i].Color = new Vector4(floatValues.ToArray());
                                break;
                            case VertexChannel.UV0:
                                cuv[i].TexCoord0 = new Vector2(floatValues.ToArray());
                                break;
                            case VertexChannel.UV1:
                                cuv[i].TexCoord1 = new Vector2(floatValues.ToArray());
                                break;
                            case VertexChannel.UV2:
                            case VertexChannel.UV3:
                            case VertexChannel.UV4:
                            case VertexChannel.UV5:
                            case VertexChannel.UV6:
                            case VertexChannel.UV7:
                            case VertexChannel.SkinWeight:
                            case VertexChannel.SkinBoneIndex:
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }

                var baseOffset = (int) (submesh.FirstByte / (mesh.IndexFormat == IndexFormat.UInt16 ? 2 : 4));

                for (var i = 0; i < submesh.IndexCount / indicesPerSurface; ++i) {
                    var indexOffset = baseOffset + i * indicesPerSurface;
                    var index = indices.Slice(indexOffset, indicesPerSurface).ToArray().Select(x => (xyvnt[x], cuv[x], default(VertexEmpty))).ToArray();

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

            foreach (var (name, texEnv) in material.SavedProperties.Textures) {
                if (texEnv.Texture.Value is not Texture2D texture) {
                    continue;
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
