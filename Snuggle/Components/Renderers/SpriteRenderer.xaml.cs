using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Snuggle.Components.Renderers;

public sealed partial class SpriteRenderer {
    public static readonly DependencyProperty CanvasBackgroundProperty = DependencyProperty.Register(nameof(CanvasBackground), typeof(Brush), typeof(SpriteRenderer), new PropertyMetadata(new SolidColorBrush(Colors.White)));
    public static readonly DependencyProperty RenderingModeProperty = DependencyProperty.Register(nameof(RenderingMode), typeof(BitmapScalingMode), typeof(SpriteRenderer), new PropertyMetadata(BitmapScalingMode.Fant));

    public SpriteRenderer() {
        InitializeComponent();
        CanvasBackground = (Brush) Application.Current.Resources["CheckerboardBrush"];
    }

    private Point Start { get; set; }
    private Point Origin { get; set; }

    public Brush CanvasBackground {
        get => (Brush) GetValue(CanvasBackgroundProperty);
        set => SetValue(CanvasBackgroundProperty, value);
    }

    public BitmapScalingMode RenderingMode {
        get => (BitmapScalingMode) GetValue(RenderingModeProperty);
        set => SetValue(RenderingModeProperty, value);
    }

    public BitmapScalingMode[] ScalingModes { get; } = { BitmapScalingMode.Linear, BitmapScalingMode.Fant, BitmapScalingMode.NearestNeighbor };

    private void Zoom(object sender, MouseWheelEventArgs e) {
        var st = (ScaleTransform) ImageView.LayoutTransform;
        var zoom = e.Delta > 0 ? .2 : -.2;
        st.ScaleY = st.ScaleX = Math.Clamp(st.ScaleX + zoom, 0.2, 10);
    }

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

    private void Reset(object sender, RoutedEventArgs e) {
        var st = (ScaleTransform) ImageView.LayoutTransform;
        st.ScaleX = 1;
        st.ScaleY = 1;
        var tt = (TranslateTransform) ImageView.RenderTransform;
        tt.X = 0;
        tt.Y = 0;

        var result = ImageView.DataContext as TaskCompletionNotifier<BitmapSource?>;
        if (result?.Task.Result is not RGBABitmapSource rgba) {
            return;
        }

        result.Result = rgba;
        result.Refresh();
        Refresh(this, new DependencyPropertyChangedEventArgs());
    }

    private void ToggleColor(object sender, RoutedEventArgs e) {
        var result = ImageView.DataContext as TaskCompletionNotifier<BitmapSource?>;
        if (result?.Task.Result is not RGBABitmapSource rgba) {
            return;
        }

        result.Result = new RGBABitmapSource(rgba) {
            HideRed = Red.IsChecked == false, HideGreen = Green.IsChecked == false, HideBlue = Blue.IsChecked == false, HideAlpha = Alpha.IsChecked == false,
        };
        result.Refresh();
    }

    private void ToggleBg(object sender, RoutedEventArgs e) {
        CanvasBackground = (Brush) Application.Current.Resources[((ToggleButton) sender).IsChecked == true ? "CheckerboardBrush" : "CheckerboardBrushWhite"];
    }

    private void Refresh(object sender, DependencyPropertyChangedEventArgs e) {
        Red.IsChecked = true;
        Green.IsChecked = true;
        Blue.IsChecked = true;
        Alpha.IsChecked = true;
    }
}
