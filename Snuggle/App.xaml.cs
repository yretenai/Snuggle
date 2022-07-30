using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Serilog;
using Serilog.Core;
using Snuggle.Core;
using Snuggle.Handlers;

namespace Snuggle;

/// <summary>Interaction logic for App.xaml</summary>
public partial class App {
    static App() {
        var _ = CoInitializeEx(IntPtr.Zero, CoInit.MultiThreaded);
    }

    public App() {
        InitializeComponent();
        Log.Logger = new LoggerConfiguration().WriteTo.Debug().WriteTo.File($"Logs/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.log").CreateLogger();
        SystemManagement.DescribeLog();
        AppDomain.CurrentDomain.UnhandledException += Crash;
        AppDomain.CurrentDomain.ProcessExit += Cleanup;
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

        Log.Fatal(ex!, "Catastrophy");
        SnuggleCore.Instance.Dispose();
    }

    private void Cleanup(object? sender, EventArgs e) {
        if (e is ExitEventArgs exitEventArgs) {
            Log.Information("Exiting ({ExitCode:X8})", exitEventArgs);
        } else {
            Log.Information("Exiting");
        }

        SnuggleCore.Instance.Dispose();
    }

    [Flags]
    private enum CoInit : uint {
        MultiThreaded = 0x00,
    }
}
