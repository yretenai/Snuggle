using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Entropy.Converters;
using Equilibrium.Implementations;
using HelixToolkit.Wpf.SharpDX;

namespace Entropy.Components.Renderers {
    public partial class MeshGeometryRenderer {
        public static RoutedCommand ToggleWireframeCommand { get; } = new();
        
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

        private void Refresh(object sender, DependencyPropertyChangedEventArgs e) {
            MeshToHelixConverter.ConvertMesh((Mesh) DataContext, Dispatcher.CurrentDispatcher, Viewport3D.Items, null);
        }

        private void ToggleWireframe(object sender, ExecutedRoutedEventArgs e) {
            if (DataContext is not Mesh) {
                return;
            }
            
            foreach (var item in  Viewport3D.Items) {
                if (item is MeshGeometryModel3D mesh) {
                    mesh.RenderWireframe = !mesh.RenderWireframe;
                }
            }
        }

        private void CanToggleWireframe(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = DataContext is Mesh && Viewport3D.Items.Any(x => x is MeshGeometryModel3D);
        }
    }
}
