using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Entropy.Handlers;
using Equilibrium.Converters;
using Equilibrium.Implementations;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using DiffuseMaterial = HelixToolkit.Wpf.SharpDX.DiffuseMaterial;

namespace Entropy.Converters {
    public static class MeshToHelixConverter {
        public static void ConvertMesh(Mesh? mesh, Dispatcher dispatcher, ObservableElement3DCollection collection) {
            if (mesh == null) {
                return;
            }

            EntropyCore.Instance.WorkerAction("DecodeMeshHelix",
                _ => {
                    if (mesh.ShouldDeserialize) {
                        mesh.Deserialize(EntropyCore.Instance.Settings.ObjectOptions);
                    }

                    var submeshes = MeshConverter.GetSubmeshes(mesh);
                    dispatcher.Invoke(() => {
                        collection.Clear();
                        if (submeshes.Count == 0) {
                            return;
                        }

                        collection.Add(new DirectionalLight3D { Color = Colors.Gray, Direction = new Vector3D(-1, -1, 0) });
                        collection.Add(new AmbientLight3D { Color = Colors.LightBlue });
                        foreach (var submesh in submeshes) {
                            collection.Add(new MeshGeometryModel3D { RenderWireframe = false, WireframeColor = Colors.Orange, Geometry = submesh.Geometry, Name = submesh.Name, Material = new DiffuseMaterial { DiffuseColor = Color4.White }, Transform = Transform3D.Identity });
                        }
                    });
                });
        }
    }
}
