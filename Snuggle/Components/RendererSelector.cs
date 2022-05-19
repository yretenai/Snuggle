using System.Windows;
using System.Windows.Controls;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;
using Snuggle.Handlers;

namespace Snuggle.Components;

public class RendererSelector : DataTemplateSelector {
    public override DataTemplate SelectTemplate(object? item, DependencyObject container) {
        if (item is SnuggleObject SnuggleObject) {
            item = SnuggleObject.GetObject();
        }

        return item switch {
            Mesh when SnuggleCore.Instance.Settings.MeshExportOptions.EnabledRenders.Contains(RendererType.Geometry) => (DataTemplate) Application.Current.Resources["SnuggleMeshGeometryRenderer"],
            GameObject when SnuggleCore.Instance.Settings.MeshExportOptions.EnabledRenders.Contains(RendererType.Geometry) => (DataTemplate) Application.Current.Resources["SnuggleMeshGeometryRenderer"],
            MeshFilter when SnuggleCore.Instance.Settings.MeshExportOptions.EnabledRenders.Contains(RendererType.Geometry) => (DataTemplate) Application.Current.Resources["SnuggleMeshGeometryRenderer"],
            MeshRenderer when SnuggleCore.Instance.Settings.MeshExportOptions.EnabledRenders.Contains(RendererType.Geometry) => (DataTemplate) Application.Current.Resources["SnuggleMeshGeometryRenderer"],
            SkinnedMeshRenderer when SnuggleCore.Instance.Settings.MeshExportOptions.EnabledRenders.Contains(RendererType.Geometry) => (DataTemplate) Application.Current.Resources["SnuggleMeshGeometryRenderer"],
            ITexture when SnuggleCore.Instance.Settings.MeshExportOptions.EnabledRenders.Contains(RendererType.Texture) => (DataTemplate) Application.Current.Resources["SnuggleTexture2DRenderer"],
            Sprite when SnuggleCore.Instance.Settings.MeshExportOptions.EnabledRenders.Contains(RendererType.Sprite) => (DataTemplate) Application.Current.Resources["SnuggleSpriteRenderer"],
            Text when SnuggleCore.Instance.Settings.MeshExportOptions.EnabledRenders.Contains(RendererType.Text) => (DataTemplate) Application.Current.Resources["SnuggleTextRenderer"],
            AudioClip when SnuggleCore.Instance.Settings.MeshExportOptions.EnabledRenders.Contains(RendererType.Audio) => (DataTemplate) Application.Current.Resources["SnuggleAudioRenderer"],
            _ => (DataTemplate) Application.Current.Resources["SnuggleNullRenderer"],
        };
    }
}
