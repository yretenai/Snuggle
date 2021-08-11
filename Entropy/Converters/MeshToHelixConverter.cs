using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Entropy.Handlers;
using Equilibrium.Converters;
using Equilibrium.Implementations;
using Equilibrium.Models.Objects.Graphics;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using SharpDX.DXGI;
using Material = Equilibrium.Implementations.Material;

namespace Entropy.Converters {
    public static class MeshToHelixConverter {
        public static void ConvertMesh(Mesh? mesh, Dispatcher dispatcher, ObservableElement3DCollection collection, List<Material?> materials, StaticBatchInfo batch) {
            if (mesh == null) {
                return;
            }

            EntropyCore.Instance.WorkerAction("DecodeMeshHelix",
                _ => {
                    if (mesh.ShouldDeserialize) {
                        mesh.Deserialize(EntropyCore.Instance.Settings.ObjectOptions);
                    }

                    List<(Texture2D? texture, byte[]? textureData)> textures = new();
                    foreach (var material in materials) {
                        UnityTexEnv? mainTexPtr = null;
                        if (material?.SavedProperties.Textures.TryGetValue("_MainTex", out mainTexPtr) == false) {
                            // ignored
                        }

                        var texture = mainTexPtr?.Texture.Value as Texture2D;
                        byte[]? textureData = null;
                        if (texture != null) {
                            if (texture.ShouldDeserialize) {
                                texture.Deserialize(EntropyCore.Instance.Settings.ObjectOptions);
                            }

                            textureData = Texture2DConverter.ToRGBA(texture).ToArray();
                            for (var i = 0; i < textureData.Length / 4; ++i) { // strip alpha
                                textureData[i * 4 + 3] = 0xFF;
                            }
                        }

                        textures.Add((texture, textureData));
                    }

                    var submeshes = MeshConverter.GetSubmeshes(mesh);
                    dispatcher.Invoke(() => {
                        var existingPointLight = collection.FirstOrDefault(x => x is PointLight3D);
                        collection.Clear();
                        if (submeshes.Count == 0) {
                            return;
                        }

                        collection.Add(existingPointLight ?? new PointLight3D { Color = Colors.White, Attenuation = new Vector3D(0.8, 0.01, 0), Position = new Point3D(0, 0, 0) });
                        for (var index = batch.FirstSubmesh; index < batch.SubmeshCount; index++) {
                            if (index >= submeshes.Count) {
                                break;
                            }

                            var submesh = submeshes[index];
                            var material3d = new PBRMaterial { AlbedoColor = Color4.White };
                            var (texture, textureData) = textures.ElementAtOrDefault(index);
                            if (texture != null &&
                                textureData?.Length > 0) {
                                material3d.AlbedoMap = new TextureModel(textureData, Format.R8G8B8A8_UNorm, texture.Width, texture.Height);
                            }

                            collection.Add(new MeshGeometryModel3D { RenderWireframe = false, WireframeColor = Colors.Orange, Geometry = submesh.Geometry, Name = submesh.Name, Material = material3d, Transform = Transform3D.Identity });
                        }
                    });
                });
        }
    }
}
