using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Entropy.Converters;
using Equilibrium.Implementations;
using Equilibrium.Models;
using Equilibrium.Models.Objects.Graphics;
using HelixToolkit.Wpf.SharpDX;
using Material = Equilibrium.Implementations.Material;

namespace Entropy.Components.Renderers {
    public partial class MeshGeometryRenderer {
        public MeshGeometryRenderer() {
            InitializeComponent();
            Refresh(this, new DependencyPropertyChangedEventArgs());
            Viewport3D.Camera.Changed += (_, _) => {
                if (Viewport3D.Items.FirstOrDefault(x => x is PointLight3D) is not PointLight3D light) {
                    return;
                }

                light.Position = Viewport3D.Camera.Position;
            };
        }

        public static RoutedCommand ToggleWireframeCommand { get; } = new();

        private void Refresh(object sender, DependencyPropertyChangedEventArgs e) {
            List<Material?> materials = new();
            Mesh? mesh = null;
            StaticBatchInfo batch;
            if (DataContext is not Mesh meshObject) {
                var gameObject = DataContext as GameObject;
                if (DataContext is Component component) {
                    gameObject = component.GameObject.Value;
                }

                if (gameObject == null) {
                    return;
                }

                var renderer = gameObject.Components.FirstOrDefault(x => x.ClassId.Equals(UnityClassId.MeshRenderer) || x.ClassId.Equals(UnityClassId.SkinnedMeshRenderer))?.Ptr.Value as Renderer;
                switch (renderer) {
                    case null:
                        return;
                    case SkinnedMeshRenderer skinnedMeshRenderer:
                        mesh = skinnedMeshRenderer.Mesh.Value;
                        break;
                    case MeshRenderer: {
                        var meshFilter = gameObject.Components.FirstOrDefault(x => x.ClassId.Equals(UnityClassId.MeshFilter))?.Ptr.Value as MeshFilter;
                        if (meshFilter != null) {
                            mesh = meshFilter.Mesh.Value;
                        }

                        break;
                    }
                }

                materials.AddRange(renderer.Materials.Select(x => x.Value));
                batch = renderer.StaticBatchInfo.SubmeshCount > 0 ? renderer.StaticBatchInfo : new StaticBatchInfo(0, (ushort) (mesh?.Submeshes.Count ?? 0));
            } else {
                mesh = meshObject;
                batch = new StaticBatchInfo(0, (ushort) mesh.Submeshes.Count);
            }

            if (mesh == null) {
                return;
            }

            MeshToHelixConverter.ConvertMesh(mesh, Dispatcher.CurrentDispatcher, Viewport3D.Items, materials, batch);
        }

        private void ToggleWireframe(object sender, ExecutedRoutedEventArgs e) {
            if (DataContext is not (Mesh or Renderer or MeshFilter or GameObject)) {
                return;
            }

            foreach (var item in Viewport3D.Items) {
                if (item is MeshGeometryModel3D mesh) {
                    mesh.RenderWireframe = !mesh.RenderWireframe;
                }
            }
        }

        private void CanToggleWireframe(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = DataContext is Mesh or Renderer or MeshFilter or GameObject && Viewport3D.Items.Any(x => x is MeshGeometryModel3D);
        }
    }
}
