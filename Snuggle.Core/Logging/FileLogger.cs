using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DragonLib;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Logging;

public sealed class FileLogger : ILogger {
    public FileLogger(Stream baseStream) => Writer = new StreamWriter(baseStream, Encoding.UTF8);
    public bool IsDisposed { get; private set; }

    private StreamWriter Writer { get; }

    public void Dispose() {
        Writer.Dispose();
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }

    public void Log(LogLevel level, string category, string message, Exception? exception) {
        if (IsDisposed) {
            return;
        }

        if (exception != null) {
            message += $"\n{exception}";
        }

        Writer.WriteLine($"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level:G}] [{category}] {message}");
        Writer.Flush();
    }

    ~FileLogger() {
        Dispose();
    }

    public static FileLogger Create(Assembly assembly, string title) {
        var logFile = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? "./", "Log", $"SnuggleLog_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():D}.log");
        logFile.EnsureDirectoryExists();
        var log = new FileLogger(new FileStream(logFile, FileMode.Create));
        var pv = FileVersionInfo.GetVersionInfo(typeof(Bundle).Assembly.Location).ProductVersion;
        var version = pv?.Contains('+') == true ? pv.Split('+', StringSplitOptions.TrimEntries).Last() : string.Empty;
        log.Log(LogLevel.Debug, "System", $"(ﾉ◕ヮ◕)ﾉ*:･ﾟ✧ {title} {version}".Trim(), null);
        log.Log(LogLevel.Debug, "System", $"net{Environment.Version.ToString()}", null);
        log.Log(LogLevel.Debug, "System", $"{Environment.OSVersion}", null);
        log.Log(LogLevel.Debug, "System", $"64-bit? {(Environment.Is64BitOperatingSystem ? "yes" : "no")}", null);
        return log;
    }
}
