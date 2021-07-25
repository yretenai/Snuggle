using System;
using System.Windows;
using System.Windows.Interop;

namespace Entropy {
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public static IntPtr HWnd => Current.MainWindow == null ? IntPtr.Zero : new WindowInteropHelper(Current.MainWindow).EnsureHandle();
    }
}
