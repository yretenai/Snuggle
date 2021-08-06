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
                Texture2D => (DataTemplate) Application.Current.Resources["EntropyTexture2DRenderer"],
                Text => (DataTemplate) Application.Current.Resources["EntropyTextRenderer"],
                _ => (DataTemplate) Application.Current.Resources["EntropyNullRenderer"],
            };
        }
    }
}
