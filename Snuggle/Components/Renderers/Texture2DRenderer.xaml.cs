using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Snuggle.Core.Interfaces;

namespace Snuggle.Components.Renderers;

public sealed partial class Texture2DRenderer {
    public static readonly DependencyProperty CanvasBackgroundProperty = DependencyProperty.Register(nameof(CanvasBackground), typeof(Brush), typeof(Texture2DRenderer), new PropertyMetadata(new SolidColorBrush(Colors.White)));
    public static readonly DependencyProperty RenderingModeProperty = DependencyProperty.Register(nameof(RenderingMode), typeof(BitmapScalingMode), typeof(Texture2DRenderer), new PropertyMetadata(BitmapScalingMode.Fant));
    public static readonly DependencyProperty FrameProperty = DependencyProperty.Register(nameof(Frame), typeof(int), typeof(Texture2DRenderer), new PropertyMetadata(0));
    public static readonly DependencyProperty FramesProperty = DependencyProperty.Register(nameof(Frames), typeof(int[]), typeof(Texture2DRenderer), new PropertyMetadata(new[] { 0 }));

    public Texture2DRenderer() {
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

    public int Frame {
        get => (int) GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    public BitmapScalingMode[] ScalingModes { get; } = { BitmapScalingMode.Linear, BitmapScalingMode.Fant, BitmapScalingMode.NearestNeighbor };

    public int[] Frames {
        get => (int[]) GetValue(FramesProperty);
        set => SetValue(FramesProperty, value);
    }

    private void Zoom(object sender, MouseWheelEventArgs e) {
        var st = (ScaleTransform) ImageView.LayoutTransform;
        var zoom = e.Delta > 0 ? .2 : -.2;
        st.ScaleX = Math.Clamp(st.ScaleX + zoom, 0.2, 10);
        st.ScaleY = 0 - st.ScaleX;
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
        st.ScaleY = 0 - st.ScaleX;
        var tt = (TranslateTransform) ImageView.RenderTransform;
        tt.X = 0;
        tt.Y = 0;

        var result = ImageView.DataContext as TaskCompletionNotifier<BitmapSource?>;
        if (result?.Task.Result is not BGRABitmapSource rgba) {
            return;
        }

        result.Result = rgba;
        Refresh(this, new DependencyPropertyChangedEventArgs());
    }

    private void ToggleColor(object sender, RoutedEventArgs e) {
        Rebuild();
    }

    private void Rebuild() {
        if (DataContext is ITexture texture) {
            Frames = Enumerable.Range(0, texture.Depth).ToArray();
        }

        var result = ImageView.DataContext as TaskCompletionNotifier<BitmapSource?>;

        if (result?.Task.IsCompleted == false) {
            return;
        }

        if (result?.Task.Result is not BGRABitmapSource rgba) {
            return;
        }

        result.Result = new BGRABitmapSource(rgba) {
            HideRed = Red.IsChecked == false, HideGreen = Green.IsChecked == false, HideBlue = Blue.IsChecked == false, HideAlpha = Alpha.IsChecked == false, Frame = Frame,
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
        Frame = 0;
        Rebuild();
    }

    private void ChangeFrame(object sender, SelectionChangedEventArgs e) {
        Rebuild();
    }
}
