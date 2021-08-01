using System;
using System.Runtime.InteropServices;

namespace Entropy {
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App {
        static App() {
            CoInitializeEx(IntPtr.Zero, CoInit.MultiThreaded);
        }

        [DllImport("Ole32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int CoInitializeEx([In, Optional] IntPtr pvReserved, [In] CoInit dwCoInit);

        [Flags]
        private enum CoInit : uint {
            MultiThreaded = 0x00,
            ApartmentThreaded = 0x02,
            DisableOLE1DDE = 0x04,
            SpeedOverMemory = 0x08,
        }
    }
}
