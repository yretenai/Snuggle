using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using DragonLib;
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
        var logFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./", "Log", $"SnuggleLog_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():D}.log");
        logFile.EnsureDirectoryExists();
        Log = new FileLogger(new FileStream(logFile, FileMode.Create));
        Log.Log(LogLevel.Debug, "System", $"(ﾉ◕ヮ◕)ﾉ*:･ﾟ✧ {SnuggleCore.GetVersion()}", null);
        Log.Log(LogLevel.Debug, "System", $"net{Environment.Version.ToString()}", null);
        Log.Log(LogLevel.Debug, "System", $"{Environment.OSVersion}", null);
        Log.Log(LogLevel.Debug, "System", $"64-bit? {(Environment.Is64BitOperatingSystem ? "yes" : "no")}", null);

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
