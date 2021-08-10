using System;
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
        public static void ConvertMesh(Mesh? mesh, Dispatcher dispatcher, ObservableElement3DCollection collection, Material? material) {
            if (mesh == null) {
                return;
            }

            EntropyCore.Instance.WorkerAction("DecodeMeshHelix",
                _ => {
                    if (mesh.ShouldDeserialize) {
                        mesh.Deserialize(EntropyCore.Instance.Settings.ObjectOptions);
                    }

                    UnityTexEnv? mainTexPtr = null;
                    if (material?.SavedProperties.Textures.TryGetValue("_MainTex", out mainTexPtr) == false) {
                        // ignored
                    }

                    var texture = mainTexPtr?.Texture.Value as Texture2D;
                    var textureData = Array.Empty<byte>();
                    if (texture != null) {
                        if (texture.ShouldDeserialize) {
                            texture.Deserialize(EntropyCore.Instance.Settings.ObjectOptions);
                        }
                        textureData = Texture2DConverter.ToRGB(texture).ToArray();
                    }

                    var submeshes = MeshConverter.GetSubmeshes(mesh);
                    dispatcher.Invoke(() => {
                        collection.Clear();
                        if (submeshes.Count == 0) {
                            return;
                        }

                        collection.Add(new PointLight3D { Color = Colors.White, Attenuation = new Vector3D(0.8, 0.01, 0), Position = new Point3D(0, 0, 0)});
                        foreach (var submesh in submeshes) {
                            var material3d = new PBRMaterial { AlbedoColor = Color4.White };
                            if (texture != null) {
                                material3d.AlbedoMap = new TextureModel(textureData, Format.R8G8B8A8_UNorm, texture.Width, texture.Height);
                            }
                            collection.Add(new MeshGeometryModel3D { RenderWireframe = false, WireframeColor = Colors.Orange, Geometry = submesh.Geometry, Name = submesh.Name, Material = material3d, Transform = Transform3D.Identity });
                        }
                    });
                });
        }
    }
}
