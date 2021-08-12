using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Entropy.Converters;
using Equilibrium.Implementations;
using HelixToolkit.Wpf.SharpDX;

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
        public static RoutedCommand ToggleLabelsCommand { get; } = new();
        public static RoutedCommand CycleSubmeshesCommand { get; } = new();
        public static RoutedCommand ZoomExtentsCommand { get; } = new();

        private void Refresh(object sender, DependencyPropertyChangedEventArgs e) {
            var existingPointLight = Viewport3D.Items.FirstOrDefault(x => x is PointLight3D);
            Viewport3D.Items.Clear();
            if (existingPointLight != null) {
                Viewport3D.Items.Add(existingPointLight);
            }

            switch (DataContext) {
                case Mesh meshObject:
                    MeshToHelixConverter.ConvertMesh(meshObject, Dispatcher.CurrentDispatcher, Viewport3D.Items);
                    break;
                case Component component:
                    DataContext = component.GameObject.Value;
                    break;
                case GameObject gameObject:
                    MeshToHelixConverter.ConvertGameObjectTree(gameObject, Dispatcher.CurrentDispatcher, Viewport3D.Items);
                    break;
            }
        }

        private void ToggleWireframe(object sender, ExecutedRoutedEventArgs e) {
            if (DataContext is not (Mesh or Renderer or MeshFilter or GameObject)) {
                return;
            }

            var meshes = CollectMeshes(Viewport3D.Items);
            foreach (var mesh in meshes) {
                mesh.RenderWireframe = !mesh.RenderWireframe;
            }
        }

        private void CycleSubmeshes(object sender, ExecutedRoutedEventArgs e) {
            if (DataContext is not (Mesh or Renderer or MeshFilter or GameObject)) {
                return;
            }

            var meshes = CollectMeshes(Viewport3D.Items);
            if (meshes.Count == 0) {
                return;
            }

            if (meshes.All(x => x.IsRendering)) {
                foreach (var mesh in meshes) {
                    mesh.IsRendering = false;
                }

                meshes[0].IsRendering = true;
                return;
            }

            var toggleNext = false;
            foreach (var mesh in meshes) {
                if (mesh.IsRendering) {
                    mesh.IsRendering = false;
                    toggleNext = true;
                    continue;
                }

                if (toggleNext) {
                    mesh.IsRendering = true;
                    return;
                }
            }

            foreach (var mesh in meshes) {
                mesh.IsRendering = true;
            }
        }

        private void ToggleLabels(object sender, ExecutedRoutedEventArgs e) {
            foreach (var item in Viewport3D.Items) {
                if (item is TopMostGroup3D topMostGroup3D) {
                    topMostGroup3D.IsRendering = !topMostGroup3D.IsRendering;
                }
            }
        }

        private static List<MeshGeometryModel3D> CollectMeshes(Collection<Element3D> collection) {
            var entries = new List<MeshGeometryModel3D>();
            foreach (var element in collection) {
                switch (element) {
                    case GroupElement3D group:
                        entries.AddRange(CollectMeshes(group.Children));
                        break;
                    case MeshGeometryModel3D model:
                        entries.Add(model);
                        break;
                }
            }

            return entries;
        }

        private void HasMeshes(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = DataContext is Mesh or Renderer or MeshFilter or GameObject;
        }

        private void ZoomExtents(object sender, RoutedEventArgs e) {
            var cache = new Dictionary<TopMostGroup3D, bool>();
            foreach (var item in Viewport3D.Items) {
                if (item is TopMostGroup3D topMostGroup3D) {
                    cache[topMostGroup3D] = topMostGroup3D.IsRendering;
                    topMostGroup3D.IsRendering = false;
                }
            }

            Viewport3D.InvalidateRender();
            Viewport3D.ZoomExtents();

            foreach (var (item, enabled) in cache) {
                item.IsRendering = enabled;
            }

            if (Viewport3D.Items.FirstOrDefault(x => x is PointLight3D) is not PointLight3D light) {
                return;
            }

            light.Position = Viewport3D.Camera.Position;
        }
    }
}
