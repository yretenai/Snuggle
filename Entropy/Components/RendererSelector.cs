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

            if (item is Texture2D) {
                return (DataTemplate) Application.Current.Resources["EntropyTexture2DRenderer"];
            }

            return (DataTemplate) Application.Current.Resources["EntropyNullRenderer"];
        }
    }
}
