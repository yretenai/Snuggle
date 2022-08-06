using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using SharpDX.DXGI;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Options;
using Snuggle.Handlers;
using Material = Snuggle.Core.Implementations.Material;
using Matrix = SharpDX.Matrix;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;
using Quaternion = SharpDX.Quaternion;
using Transform = Snuggle.Core.Implementations.Transform;

namespace Snuggle.Converters;

public static partial class MeshToHelixConverter {

    [RegexGenerator("[^a-zA-Z0-9_]", RegexOptions.Compiled)]
    private static partial Regex XAMLSafeCharactersRegex();
    private static readonly Regex XAMLSafeCharacters = XAMLSafeCharactersRegex();

    private static List<Object3D> GetSubmeshes(Mesh mesh, CancellationToken token) {
        if (mesh.ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        var vertexStream = MeshConverter.GetVBO(mesh, out _, out var descriptors, out var strides);
        var indexStream = MeshConverter.GetIBO(mesh);

        var objects = new List<Object3D>();
        var options = SnuggleCore.Instance.Settings.MeshExportOptions;
        for (var index = 0; index < mesh.Submeshes.Count; index++) {
            if (token.IsCancellationRequested) {
                return objects;
            }

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
                if (token.IsCancellationRequested) {
                    return objects;
                }

                for (var channel = 0; channel < descriptors.Length; channel++) {
                    var info = descriptors[channel];
                    if (token.IsCancellationRequested) {
                        return objects;
                    }

                    if (info == null) {
                        continue;
                    }

                    var stride = strides[info.Stream];
                    var offset = (submesh.FirstVertex + i) * stride;
                    var data = vertexStream[info.Stream][(offset + info.Offset)..].Span;
                    if (info.Dimension == VertexDimension.None) {
                        continue;
                    }

                    var value = info.Unpack(data);
                    var floatValues = value.Select(Convert.ToSingle).Concat(new float[4]);
                    switch ((VertexChannel) channel) {
                        case VertexChannel.Vertex: {
                            var vec = new Vector3(floatValues.Take(3).ToArray());
                            if (options.MirrorXPosition) {
                                vec.X *= -1;
                            }

                            geometry.Positions.Add(vec);
                            break;
                        }
                        case VertexChannel.Normal: {
                            var vec = new Vector3(floatValues.Take(3).ToArray());
                            if (options.MirrorXNormal) {
                                vec.X *= -1;
                            }

                            geometry.Normals.Add(vec);
                            break;
                        }
                        case VertexChannel.Color:
                            geometry.Colors.Add(new Color4(floatValues.Take(4).ToArray()));
                            break;
                        case VertexChannel.UV0:
                            geometry.TextureCoordinates.Add(new Vector2(floatValues.Take(2).ToArray()));
                            break;
                        case VertexChannel.Tangent:
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

    public static void ConvertGameObjectTree(GameObject? gameObject, Dispatcher dispatcher, Collection<Element3D> collection, CancellationToken token1) {
        SnuggleCore.Instance.WorkerAction(
            "DecodeGeometryHelix",
            token2 => {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(token1, token2);
                gameObject = SnuggleMeshFile.FindTopGeometry(gameObject, SnuggleCore.Instance.Settings.MeshExportOptions.FindGameObjectParents);
                if (gameObject == null) {
                    return;
                }

                var meshData = new Dictionary<(long, string), (List<Object3D> submeshes, List<(ITexture? texture, Memory<byte> textureData)>)>();
                FindGeometryMeshData(gameObject, meshData, cts.Token);

                dispatcher.Invoke(
                    () => {
                        var existingPointLight = collection.FirstOrDefault(x => x is PointLight3D);
                        collection.Clear();
                        collection.Add(existingPointLight ?? new PointLight3D { Color = Colors.White, Attenuation = new Vector3D(0.8, 0.01, 0), Position = new Point3D(0, 0, 0) });
                        var lines = new LineGeometryModel3D { EnableViewFrustumCheck = false };
                        var lineBuilder = new LineBuilder();
                        var labels = new BillboardTextModel3D { EnableViewFrustumCheck = false };
                        var labelGeometry = new BillboardText3D();
                        labels.Geometry = labelGeometry;
                        var topMost = new TopMostGroup3D { EnableTopMost = true };
                        collection.Add(topMost);
                        AddGameObjectNode(
                            gameObject,
                            collection,
                            Matrix.Identity,
                            lineBuilder,
                            labelGeometry,
                            meshData,
                            cts.Token);
                        lines.Geometry = lineBuilder.ToLineGeometry3D();
                        lines.Color = Colors.Crimson;
                        if (SnuggleCore.Instance.Settings.MeshExportOptions.DisplayRelationshipLines) {
                            topMost.Children.Add(lines);
                        }

                        if (SnuggleCore.Instance.Settings.MeshExportOptions.DisplayLabels) {
                            topMost.Children.Add(labels);
                        }
                    });
            },
            true);
    }

    private static void FindGeometryMeshData(GameObject gameObject, IDictionary<(long, string), (List<Object3D> submeshes, List<(ITexture? texture, Memory<byte> textureData)>)> meshData, CancellationToken token) {
        var submeshes = new List<Object3D>();
        if (gameObject.FindComponent(UnityClassId.MeshFilter).Value is MeshFilter filter && filter.Mesh.Value != null) {
            filter.Mesh.Value.Deserialize(ObjectDeserializationOptions.Default);
            submeshes = GetSubmeshes(filter.Mesh.Value, token);
        }

        var textureData = new List<(ITexture? texture, Memory<byte> textureData)>();
        if (gameObject.FindComponent(UnityClassId.MeshRenderer, UnityClassId.SkinnedMeshRenderer).Value is Renderer renderer) {
            textureData = FindTextureData(renderer.Materials.Select(x => x.Value), token);

            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer && skinnedMeshRenderer.Mesh.Value != null) {
                skinnedMeshRenderer.Mesh.Value.Deserialize(ObjectDeserializationOptions.Default);
                submeshes = GetSubmeshes(skinnedMeshRenderer.Mesh.Value, token);
            }
        }

        if (submeshes.Count > 0) {
            meshData[gameObject.GetCompositeId()] = (submeshes, textureData);
        }

        if (SnuggleCore.Instance.Settings.MeshExportOptions.FindGameObjectDescendants) {
            foreach (var child in gameObject.Children) {
                if (child.Value == null) {
                    continue;
                }

                if (token.IsCancellationRequested) {
                    return;
                }

                FindGeometryMeshData(child.Value, meshData, token);
            }
        }
    }

    private static void AddGameObjectNode(
        GameObject gameObject,
        ICollection<Element3D> collection,
        Matrix? parentMatrix,
        LineBuilder builder,
        BillboardText3D labels,
        IReadOnlyDictionary<(long, string), (List<Object3D> submeshes, List<(ITexture? texture, Memory<byte> textureData)>)> meshData,
        CancellationToken token) {
        if (gameObject.FindComponent(UnityClassId.Transform).Value is not Transform transform) {
            return;
        }

        var (rX, rY, rZ, rW) = transform.Rotation;
        var (sX, sY, sZ) = transform.Scale;
        var (tX, tY, tZ) = transform.Translation;
        var localMatrix = Matrix.Scaling(sX, sY, sZ) * Matrix.RotationQuaternion(new Quaternion(rX, rY, rZ, rW)) * Matrix.Translation(new Vector3(tX, tY, tZ));
        if (SnuggleCore.Instance.Settings.MeshExportOptions.MirrorXPosition) {
            var mirror = Matrix.Scaling(-1, 1, 1);
            localMatrix = mirror * localMatrix * mirror;
        }

        var matrix = localMatrix * (parentMatrix ?? Matrix.Identity);

        if (parentMatrix.HasValue) {
            builder.AddLine(matrix.TranslationVector, parentMatrix.Value.TranslationVector);
            labels.TextInfo.Add(new TextInfo(gameObject.Name, matrix.TranslationVector) { Foreground = new Color4(1, 1, 1, 0.5f), Scale = 0.5f });
        }

        if (meshData.TryGetValue(gameObject.GetCompositeId(), out var mesh)) {
            var group = new GroupModel3D();
            group.SceneNode.ModelMatrix = matrix;
            var (submeshes, textures) = mesh;

            BuildSubmeshes(group.Children, submeshes, textures, token);
            collection.Add(group);
        }

        foreach (var child in gameObject.Children) {
            if (child.Value == null) {
                continue;
            }

            if (token.IsCancellationRequested) {
                return;
            }

            AddGameObjectNode(
                child.Value,
                collection,
                matrix,
                builder,
                labels,
                meshData,
                token);
        }
    }

    public static void ConvertMesh(Mesh? mesh, Dispatcher dispatcher, Collection<Element3D> collection, CancellationToken token) {
        if (mesh == null) {
            return;
        }

        SnuggleCore.Instance.WorkerAction(
            "DecodeMeshHelix",
            _ => {
                mesh.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);
                var submeshes = GetSubmeshes(mesh, token);
                dispatcher.Invoke(
                    () => {
                        var existingPointLight = collection.FirstOrDefault(x => x is PointLight3D);
                        collection.Clear();
                        collection.Add(existingPointLight ?? new PointLight3D { Color = Colors.White, Attenuation = new Vector3D(0.8, 0.01, 0), Position = new Point3D(0, 0, 0) });
                        if (submeshes.Count == 0) {
                            return;
                        }

                        if (token.IsCancellationRequested) {
                            return;
                        }

                        BuildSubmeshes(collection, submeshes, null, token);
                    });
            },
            true);
    }

    private static void BuildSubmeshes(ICollection<Element3D> collection, IReadOnlyList<Object3D> submeshes, IReadOnlyCollection<(ITexture? texture, Memory<byte> textureData)>? textures, CancellationToken token) {
        textures ??= new List<(ITexture? texture, Memory<byte> textureData)>();
        for (var index = 0; index < submeshes.Count; index++) {
            if (token.IsCancellationRequested) {
                return;
            }

            if (index >= submeshes.Count) {
                break;
            }

            var submesh = submeshes[index];
            var material3d = new PBRMaterial { AlbedoColor = Color4.White };
            var (texture, textureData) = textures.ElementAtOrDefault(index);
            if (texture != null && !textureData.IsEmpty) {
                material3d.AlbedoMap = new TextureModel(textureData.ToArray(), Format.R8G8B8A8_UNorm, texture.Width, texture.Height);
            }

            collection.Add(
                new MeshGeometryModel3D {
                    RenderWireframe = SnuggleCore.Instance.Settings.MeshExportOptions.DisplayWireframe,
                    WireframeColor = Colors.Red,
                    Geometry = submesh.Geometry,
                    Name = XAMLSafeCharacters.Replace(submesh.Name, "_"),
                    Material = material3d,
                    Transform = Transform3D.Identity,
                });
        }
    }

    private static List<(ITexture? texture, Memory<byte> textureData)> FindTextureData(IEnumerable<Material?> materials, CancellationToken token) {
        List<(ITexture? texture, Memory<byte> textureData)> textures = new();
        foreach (var material in materials) {
            if (token.IsCancellationRequested) {
                return textures;
            }

            if (material == null) {
                continue;
            }

            if (material.ShouldDeserialize) {
                material.Deserialize(ObjectDeserializationOptions.Default);
            }

            if (material.SavedProperties!.Textures.TryGetValue("_MainTex", out var mainTexPtr) == false) {
                mainTexPtr = material.SavedProperties.Textures.FirstOrDefault().Value;
            }

            var texture = mainTexPtr.Texture.Value as ITexture;
            var textureData = Memory<byte>.Empty;
            if (texture != null) {
                texture.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);
                textureData = SnuggleTextureFile.LoadCachedTexture(texture, SnuggleCore.Instance.Settings.ExportOptions.UseTextureDecoder);
                for (var i = 0; i < textureData.Length / 4; ++i) { // strip alpha
                    var bytes = textureData.Span.Slice(i * 4, 4).ToArray();
                    if (texture.TextureFormat.IsAlphaFirst()) {
                        textureData.Span[i * 4 + 0] = bytes[1];
                        textureData.Span[i * 4 + 1] = bytes[2];
                        textureData.Span[i * 4 + 2] = bytes[3];
                    } else if (texture.TextureFormat.IsBGRA(SnuggleCore.Instance.Settings.ExportOptions.UseTextureDecoder)) {
                        textureData.Span[i * 4 + 0] = bytes[2];
                        textureData.Span[i * 4 + 1] = bytes[1];
                        textureData.Span[i * 4 + 2] = bytes[0];
                    }

                    textureData.Span[i * 4 + 3] = 0xFF;
                }
            }

            textures.Add((texture, textureData));
        }

        return textures;
    }
}
