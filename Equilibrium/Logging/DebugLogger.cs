using System;
using System.Diagnostics;
using DragonLib;
using Equilibrium.Interfaces;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Logging {
    [PublicAPI]
    public class DebugLogger : Singleton<DebugLogger>, ILogger {
        public void Log(LogLevel level, string? category, string message, Exception? exception) {
            if (exception != null) {
                message += $"\n{exception}";
            }

            Debug.WriteLine(string.IsNullOrEmpty(category) ? $"[{level:G}] {message}" : $"[{level:G}][{category}] {message}");
        }
    }
}
