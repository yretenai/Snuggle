using System;
using DragonLib;
using Equilibrium.Interfaces;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Logging {
    [PublicAPI]
    public class ConsoleLogger : Singleton<ConsoleLogger>, ILogger {
        public void Log(LogLevel level, string? category, string message, Exception? exception) {
            var target = level >= LogLevel.Error ? Console.Error : Console.Out;
            if (exception != null) {
                message += $"\n{exception}";
            }

            target.WriteLine(string.IsNullOrEmpty(category) ? $"[{level:G}] {message}" : $"[{level:G}][{category}] {message}");
        }
    }
}
