using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace Snuggle; 

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App {
    static App() {
        var _ = CoInitializeEx(IntPtr.Zero, CoInit.MultiThreaded);
    }

    [DllImport("Ole32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int CoInitializeEx([In, Optional] IntPtr pvReserved, [In] CoInit dwCoInit);

    public static void OpenWindow<T>() where T : Window, new() {
        var existing = Current.Windows.OfType<T>().FirstOrDefault();
        if (existing == null) {
            new T().Show();
        } else {
            existing.Focus();
        }
    }

    [Flags]
    private enum CoInit : uint {
        MultiThreaded = 0x00,
    }
}