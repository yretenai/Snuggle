using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Logging;

[PublicAPI]
public sealed class FileLogger : ILogger {
    public bool IsDisposed { get; private set; }
    public FileLogger(Stream baseStream) => Writer = new StreamWriter(baseStream, Encoding.UTF8);

    private StreamWriter Writer { get; }

    public void Dispose() {
        Writer.Dispose();
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }

    public void Log(LogLevel level, string? category, string message, Exception? exception) {
        if (IsDisposed) {
            return;
        }
        if (exception != null) {
            message += $"\n{exception}";
        }

        Writer.WriteLine(string.IsNullOrEmpty(category) ? $"[{level:G}] {message}" : $"[{level:G}][{category}] {message}");
        Writer.Flush();
    }

    ~FileLogger() {
        Dispose();
    }
}
