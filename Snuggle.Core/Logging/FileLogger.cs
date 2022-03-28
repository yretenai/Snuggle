using System;
using System.IO;
using System.Text;
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
}
