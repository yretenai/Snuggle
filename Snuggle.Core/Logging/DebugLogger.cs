using System;
using System.Diagnostics;
using DragonLib;
using JetBrains.Annotations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Logging;

[PublicAPI]
public class DebugLogger : Singleton<DebugLogger>, ILogger {
    public void Log(LogLevel level, string? category, string message, Exception? exception) {
        if (exception != null) {
            message += $"\n{exception}";
        }

        Debug.WriteLine(string.IsNullOrEmpty(category) ? $"[{level:G}] {message}" : $"[{level:G}][{category}] {message}");
    }
}
