using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Snuggle.Core.Logging;
using Snuggle.Core.Meta;
using Snuggle.Handlers;

namespace Snuggle;

/// <summary>Interaction logic for App.xaml</summary>
public partial class App {
    static App() {
        var _ = CoInitializeEx(IntPtr.Zero, CoInit.MultiThreaded);
    }

    public App() {
        InitializeComponent();
        Log = FileLogger.Create(Assembly.GetExecutingAssembly(), SnuggleCore.BaseTitle);
        AppDomain.CurrentDomain.UnhandledException += Crash;
        AppDomain.CurrentDomain.ProcessExit += Cleanup;
    }

    public FileLogger Log { get; }

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

    private void Crash(object sender, EventArgs e) {
        Exception? ex = default;
        switch (e) {
            case UnhandledExceptionEventArgs { IsTerminating: false }:
                return;
            case UnhandledExceptionEventArgs ueea:
                ex = ueea.ExceptionObject as Exception;
                break;
            case DispatcherUnhandledExceptionEventArgs dueea:
                ex = dueea.Exception;
                break;
        }

        Log.Log(LogLevel.Crash, "System", "Unrecoverable crash", ex);
        SnuggleCore.Instance.Dispose();
        Log.Dispose();
    }

    private void Cleanup(object? sender, EventArgs e) {
        if (e is ExitEventArgs exitEventArgs) {
            Log.Log(LogLevel.Info, "System", $"Exiting ({exitEventArgs:X8})...", null);
        } else {
            Log.Log(LogLevel.Info, "System", "Exiting...", null);
        }

        SnuggleCore.Instance.Dispose();
        Log.Dispose();
    }

    [Flags]
    private enum CoInit : uint {
        MultiThreaded = 0x00,
    }
}
