using System;
using DragonLib;
using JetBrains.Annotations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Logging;

[PublicAPI]
public sealed class ConsoleLogger : Singleton<ConsoleLogger>, ILogger {
    public void Log(LogLevel level, string? category, string message, Exception? exception) {
        var target = level >= LogLevel.Error ? Console.Error : Console.Out;
        if (exception != null) {
            message += $"\n{exception}";
        }

        target.WriteLine(string.IsNullOrEmpty(category) ? $"[{level:G}] {message}" : $"[{level:G}][{category}] {message}");
    }

    public void Dispose() { }
}
