using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Entropy.Handlers;
using Equilibrium.Converters;
using Equilibrium.Exceptions;
using Equilibrium.Implementations;
using Equilibrium.Models;
using Equilibrium.Models.Objects.Graphics;
using Equilibrium.Options;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using SharpDX.DXGI;
using Material = Equilibrium.Implementations.Material;
using Matrix = SharpDX.Matrix;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;
using Quaternion = SharpDX.Quaternion;
using Transform = Equilibrium.Implementations.Transform;

namespace Entropy.Converters {
    public static class MeshToHelixConverter {
        public static List<Object3D> GetSubmeshes(Mesh mesh) {
            if (mesh.ShouldDeserialize) {
                throw new IncompleteDeserializationException();
            }

            var vertexStream = MeshConverter.GetVBO(mesh, out var descriptors, out var strides);
            var indexStream = MeshConverter.GetIBO(mesh);
            
            var objects = new List<Object3D>();
            for (var index = 0; index < mesh.Submeshes.Count; index++) {
                var submesh = mesh.Submeshes[index];
                var geometry = new MeshGeometry3D();
                var span = indexStream.Span.Slice((int) submesh.FirstByte, (int) (submesh.IndexCount * (mesh.IndexFormat == IndexFormat.UInt16 ? 2 : 4)));
                var indices = mesh.IndexFormat == IndexFormat.UInt16 ? MemoryMarshal.Cast<byte, ushort>(span).ToArray().Select(x => (int) x).ToArray() : MemoryMarshal.Cast<byte, int>(span).ToArray();
                if (submesh.FirstByte > 0) {
                    var baseIndex = indices.Min();
                    for (var indiceIndex = 0; indiceIndex < submesh.IndexCount; ++indiceIndex) {
                        indices[indiceIndex] -= baseIndex;
                    }
                }

                geometry.Indices = new IntCollection(indices);
                geometry.Positions = new Vector3Collection();
                geometry.Positions.EnsureCapacity(submesh.VertexCount);
                geometry.Normals = new Vector3Collection();
                geometry.Normals.EnsureCapacity(submesh.VertexCount);
                geometry.Tangents = new Vector3Collection();
                geometry.Tangents.EnsureCapacity(submesh.VertexCount);
                geometry.Colors = new Color4Collection();
                geometry.Colors.EnsureCapacity(submesh.VertexCount);
                geometry.TextureCoordinates = new Vector2Collection();
                geometry.TextureCoordinates.EnsureCapacity(submesh.VertexCount);
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
                        var uintValues = value.Select(Convert.ToUInt32).Concat(new uint[4]);
                        switch (channel) {
                            case VertexChannel.Vertex:
                                geometry.Positions.Add(new Vector3(floatValues.Take(3).ToArray()));
                                break;
                            case VertexChannel.Normal:
                                geometry.Normals.Add(new Vector3(floatValues.Take(3).ToArray()));
                                break;
                            case VertexChannel.Tangent:
                                geometry.Tangents.Add(new Vector3(floatValues.Take(3).ToArray()));
                                break;
                            case VertexChannel.Color:
                                geometry.Colors.Add(new Color4(uintValues.ElementAt(0)));
                                break;
                            case VertexChannel.UV0:
                                geometry.TextureCoordinates.Add(new Vector2(floatValues.Take(2).ToArray()));
                                break;
                            case VertexChannel.UV1:
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

                objects.Add(new Object3D { Name = $"{mesh.Name}_Submesh{index}", Geometry = geometry });
            }

            return objects;
        }

        public static void ConvertGameObjectTree(GameObject? gameObject, Dispatcher dispatcher, Collection<Element3D> collection) {
            EntropyCore.Instance.WorkerAction("DecodeGeometryHelix",
                _ => {
                    gameObject = FindTopGeometry(gameObject);
                    if (gameObject == null) {
                        return;
                    }

                    var meshData = new Dictionary<long, (List<Object3D> submeshes, List<(Texture2D? texture, Memory<byte> textureData)>)>();
                    FindGeometryMeshData(gameObject, meshData);

                    dispatcher.Invoke(() => {
                        var existingPointLight = collection.FirstOrDefault(x => x is PointLight3D);
                        collection.Clear();
                        collection.Add(existingPointLight ?? new PointLight3D { Color = Colors.White, Attenuation = new Vector3D(0.8, 0.01, 0), Position = new Point3D(0, 0, 0) });
                        var lines = new LineGeometryModel3D { EnableViewFrustumCheck = false };
                        var lineBuilder = new LineBuilder();
                        var labels = new BillboardTextModel3D { EnableViewFrustumCheck = false };
                        var labelGeometry = new BillboardText3D();
                        labels.Geometry = labelGeometry;
                        var topMost = new TopMostGroup3D {
                            EnableTopMost = true,
                        };
                        collection.Add(topMost);
                        AddGameObjectNode(gameObject, collection, Matrix.Identity, lineBuilder, labelGeometry, meshData);
                        lines.Geometry = lineBuilder.ToLineGeometry3D();
                        lines.Color = Colors.Crimson;
                        topMost.Children.Add(lines);
                        topMost.Children.Add(labels);
                    });
                });
        }

        private static void FindGeometryMeshData(GameObject gameObject,
            IDictionary<long, (List<Object3D> submeshes, List<(Texture2D? texture, Memory<byte> textureData)>)> meshData) {
            if (gameObject.FindComponent(UnityClassId.Transform).Value is not Transform transform) {
                return;
            }

            var submeshes = new List<Object3D>();
            if (gameObject.FindComponent(UnityClassId.MeshFilter).Value is MeshFilter filter &&
                filter.Mesh.Value != null) {
                if (filter.Mesh.Value.ShouldDeserialize) {
                    filter.Mesh.Value.Deserialize(ObjectDeserializationOptions.Default);
                }

                submeshes = GetSubmeshes(filter.Mesh.Value);
            }

            var textureData = new List<(Texture2D? texture, Memory<byte> textureData)>();
            if (gameObject.FindComponent(UnityClassId.MeshRenderer, UnityClassId.SkinnedMeshRenderer).Value is Renderer renderer) {
                textureData = FindTextureData(renderer.Materials.Select(x => x.Value));

                if (renderer is SkinnedMeshRenderer skinnedMeshRenderer &&
                    skinnedMeshRenderer.Mesh.Value != null) {
                    if (skinnedMeshRenderer.Mesh.Value.ShouldDeserialize) {
                        skinnedMeshRenderer.Mesh.Value.Deserialize(ObjectDeserializationOptions.Default);
                    }

                    submeshes = GetSubmeshes(skinnedMeshRenderer.Mesh.Value);
                }
            }

            if (submeshes.Count > 0) {
                meshData[gameObject.PathId] = (submeshes, textureData);
            }

            foreach (var child in transform.Children) {
                if (child.Value?.GameObject.Value == null) {
                    continue;
                }

                FindGeometryMeshData(child.Value.GameObject.Value, meshData);
            }
        }

        private static void AddGameObjectNode(GameObject gameObject, Collection<Element3D> collection, Matrix? parentMatrix, LineBuilder builder, BillboardText3D labels, Dictionary<long, (List<Object3D> submeshes, List<(Texture2D? texture, Memory<byte> textureData)>)> meshData) {
            if (gameObject.FindComponent(UnityClassId.Transform).Value is not Transform transform) {
                return;
            }

            var (rX, rY, rZ, rW) = transform.Rotation;
            var (sX, sY, sZ) = transform.Scale;
            var (tX, tY, tZ) = transform.Translation;

            var localMatrix = Matrix.Scaling(sX, sY, sZ) * Matrix.RotationQuaternion(new Quaternion(rX, -rY, -rZ, rW)) * Matrix.Translation(new Vector3(-tX, tY, tZ));
            var matrix = localMatrix * (parentMatrix ?? Matrix.Identity);

            if (parentMatrix.HasValue) {
                builder.AddLine(matrix.TranslationVector, parentMatrix.Value.TranslationVector);
                labels.TextInfo.Add(new TextInfo(gameObject.Name, matrix.TranslationVector) { Foreground = new Color4(1, 1, 1, 0.5f), Scale = 0.5f });
            }

            if (meshData.TryGetValue(gameObject.PathId, out var mesh)) {
                var group = new GroupModel3D();
                group.SceneNode.ModelMatrix = matrix;
                var (submeshes, textures) = mesh;

                BuildSubmeshes(group.Children, submeshes, textures);
                collection.Add(group);
            }

            foreach (var child in transform.Children) {
                if (child.Value?.GameObject.Value == null) {
                    continue;
                }

                AddGameObjectNode(child.Value.GameObject.Value, collection, matrix, builder, labels, meshData);
            }
        }

        private static GameObject? FindTopGeometry(GameObject? gameObject) {
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

        public static void ConvertMesh(Mesh? mesh, Dispatcher dispatcher, Collection<Element3D> collection) {
            if (mesh == null) {
                return;
            }

            EntropyCore.Instance.WorkerAction("DecodeMeshHelix",
                _ => {
                    if (mesh.ShouldDeserialize) {
                        mesh.Deserialize(EntropyCore.Instance.Settings.ObjectOptions);
                    }

                    var submeshes = GetSubmeshes(mesh);
                    dispatcher.Invoke(() => {
                        var existingPointLight = collection.FirstOrDefault(x => x is PointLight3D);
                        collection.Clear();
                        collection.Add(existingPointLight ?? new PointLight3D { Color = Colors.White, Attenuation = new Vector3D(0.8, 0.01, 0), Position = new Point3D(0, 0, 0) });
                        if (submeshes.Count == 0) {
                            return;
                        }

                        BuildSubmeshes(collection, submeshes);
                    });
                });
        }

        private static unsafe void BuildSubmeshes(ICollection<Element3D> collection, IReadOnlyList<Object3D> submeshes, IReadOnlyCollection<(Texture2D? texture, Memory<byte> textureData)>? textures = null) {
            textures ??= new List<(Texture2D? texture, Memory<byte> textureData)>();
            for (var index = 0; index < submeshes.Count; index++) {
                if (index >= submeshes.Count) {
                    break;
                }

                var submesh = submeshes[index];
                var material3d = new PBRMaterial { AlbedoColor = Color4.White };
                var (texture, textureData) = textures.ElementAtOrDefault(index);
                if (texture != null &&
                    !textureData.IsEmpty) {
                    material3d.AlbedoMap = new TextureModel((IntPtr) textureData.Pin().Pointer, Format.R8G8B8A8_UNorm, texture.Width, texture.Height);
                }

                collection.Add(new MeshGeometryModel3D {
                    RenderWireframe = false,
                    WireframeColor = Colors.Orange,
                    Geometry = submesh.Geometry,
                    Name = submesh.Name,
                    Material = material3d,
                    Transform = Transform3D.Identity,
                });
            }
        }

        private static List<(Texture2D? texture, Memory<byte> textureData)> FindTextureData(IEnumerable<Material?> materials) {
            List<(Texture2D? texture, Memory<byte> textureData)> textures = new();
            foreach (var material in materials) {
                UnityTexEnv? mainTexPtr = null;
                if (material?.SavedProperties.Textures.TryGetValue("_MainTex", out mainTexPtr) == false) {
                    // ignored
                }

                var texture = mainTexPtr?.Texture.Value as Texture2D;
                var textureData = Memory<byte>.Empty;
                if (texture != null) {
                    if (texture.ShouldDeserialize) {
                        texture.Deserialize(EntropyCore.Instance.Settings.ObjectOptions);
                    }

                    textureData = EntropyTextureFile.LoadCachedTexture(texture);
                    for (var i = 0; i < textureData.Length / 4; ++i) { // strip alpha
                        textureData.Span[i * 4 + 3] = 0xFF;
                    }
                }

                textures.Add((texture, textureData));
            }

            return textures;
        }
    }
}
