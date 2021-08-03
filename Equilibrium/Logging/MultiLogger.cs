using System;
using System.Collections.Generic;
using Equilibrium.Interfaces;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Logging {
    [PublicAPI]
    public class MultiLogger : ILogger {
        public List<ILogger> Loggers { get; set; } = new();

        public void Log(LogLevel level, string? category, string message, Exception? exception) {
            foreach (var logger in Loggers) {
                logger.Log(level, category, message, exception);
            }
        }
    }
}
