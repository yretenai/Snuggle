using System.Windows;
using System.Windows.Controls;
using Entropy.Handlers;
using Equilibrium.Implementations;

namespace Entropy.Components {
    public class RendererSelector : DataTemplateSelector {
        public override DataTemplate SelectTemplate(object? item, DependencyObject container) {
            if (item is EntropyObject entropyObject) {
                item = entropyObject.GetObject();
            }

            return item switch {
                Mesh when EntropyCore.Instance.Settings.EnabledRenders.Contains(RendererType.Geometry) => (DataTemplate) Application.Current.Resources["EntropyMeshGeometryRenderer"],
                GameObject when EntropyCore.Instance.Settings.EnabledRenders.Contains(RendererType.Geometry) => (DataTemplate) Application.Current.Resources["EntropyMeshGeometryRenderer"],
                MeshFilter when EntropyCore.Instance.Settings.EnabledRenders.Contains(RendererType.Geometry) => (DataTemplate) Application.Current.Resources["EntropyMeshGeometryRenderer"],
                MeshRenderer when EntropyCore.Instance.Settings.EnabledRenders.Contains(RendererType.Geometry) => (DataTemplate) Application.Current.Resources["EntropyMeshGeometryRenderer"],
                SkinnedMeshRenderer when EntropyCore.Instance.Settings.EnabledRenders.Contains(RendererType.Geometry) => (DataTemplate) Application.Current.Resources["EntropyMeshGeometryRenderer"],
                Texture2D when EntropyCore.Instance.Settings.EnabledRenders.Contains(RendererType.Texture) => (DataTemplate) Application.Current.Resources["EntropyTexture2DRenderer"],
                Text when EntropyCore.Instance.Settings.EnabledRenders.Contains(RendererType.Text) => (DataTemplate) Application.Current.Resources["EntropyTextRenderer"],
                _ => (DataTemplate) Application.Current.Resources["EntropyNullRenderer"],
            };
        }
    }
}
