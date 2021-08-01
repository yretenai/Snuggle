using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Entropy.Components.Renderers {
    public sealed partial class Texture2DRenderer {
        public Texture2DRenderer() {
            InitializeComponent();
        }

        private void Zoom(object sender, MouseWheelEventArgs e) {
            var st = (ScaleTransform) ImageView.LayoutTransform;
            var zoom = e.Delta > 0 ? .2 : -.2;
            st.ScaleX = Math.Clamp(st.ScaleX + zoom, 0.2, 3);
            st.ScaleY = 0 - st.ScaleX;
        }

        private Point Start { get; set; }
        private Point Origin { get; set; }

        private void CapturePan(object sender, MouseButtonEventArgs e) {
            ImageView.CaptureMouse();
            var tt = (TranslateTransform) ImageView.RenderTransform;
            Start = e.GetPosition(Root);
            Origin = new Point(tt.X, tt.Y);
        }

        private void Pan(object sender, MouseEventArgs e) {
            if (ImageView.IsMouseCaptured) {
                var tt = (TranslateTransform) ImageView.RenderTransform;
                var v = Start - e.GetPosition(Root);
                tt.X = Origin.X - v.X;
                tt.Y = Origin.Y - v.Y;
            }
        }

        private void ReleasePan(object sender, MouseButtonEventArgs e) {
            ImageView.ReleaseMouseCapture();
        }
    }
}
