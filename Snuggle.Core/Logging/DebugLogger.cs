using System;
using System.Diagnostics;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Logging;

public sealed class DebugLogger : Singleton<DebugLogger>, ILogger {
    public void Log(LogLevel level, string category, string message, Exception? exception) {
        if (exception != null) {
            message += $"\n{exception}";
        }

        Debug.WriteLine($"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level:G}] [{category}] {message}");
    }

    public void Dispose() { }
}
