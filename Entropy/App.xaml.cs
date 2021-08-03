using System;
using System.Runtime.InteropServices;

namespace Entropy {
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App {
        static App() {
            var _ = CoInitializeEx(IntPtr.Zero, CoInit.MultiThreaded);
        }

        [DllImport("Ole32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int CoInitializeEx([In, Optional] IntPtr pvReserved, [In] CoInit dwCoInit);

        [Flags]
        private enum CoInit : uint {
            MultiThreaded = 0x00,
        }
    }
}
