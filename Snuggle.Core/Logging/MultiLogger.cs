using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Logging;

[PublicAPI]
public class MultiLogger : ILogger {
    public List<ILogger> Loggers { get; set; } = new();

    public void Log(LogLevel level, string? category, string message, Exception? exception) {
        foreach (var logger in Loggers) {
            logger.Log(level, category, message, exception);
        }
    }

    public void Dispose() {
        foreach (var logger in Loggers) {
            logger.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    ~MultiLogger() {
        Dispose();
    }
}
