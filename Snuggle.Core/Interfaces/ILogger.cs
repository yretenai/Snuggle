using System;
using JetBrains.Annotations;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Interfaces {
    [PublicAPI]
    public interface ILogger {
        public void Log(LogLevel level, string? category, string message, Exception? exception);

        public void Debug(string category, string message, Exception exception) => Log(LogLevel.Debug, category, message, exception);
        public void Info(string category, string message, Exception exception) => Log(LogLevel.Info, category, message, exception);
        public void Warning(string category, string message, Exception exception) => Log(LogLevel.Warning, category, message, exception);
        public void Error(string category, string message, Exception exception) => Log(LogLevel.Error, category, message, exception);
        public void Crash(string category, string message, Exception exception) => Log(LogLevel.Crash, category, message, exception);

        public void Debug(string category, string message) => Log(LogLevel.Debug, category, message, null);
        public void Info(string category, string message) => Log(LogLevel.Info, category, message, null);
        public void Warning(string category, string message) => Log(LogLevel.Warning, category, message, null);
        public void Error(string category, string message) => Log(LogLevel.Error, category, message, null);
        public void Crash(string category, string message) => Log(LogLevel.Crash, category, message, null);

        public void Debug(string message) => Log(LogLevel.Debug, null, message, null);
        public void Info(string message) => Log(LogLevel.Info, null, message, null);
        public void Warning(string message) => Log(LogLevel.Warning, null, message, null);
        public void Error(string message) => Log(LogLevel.Error, null, message, null);
        public void Crash(string message) => Log(LogLevel.Crash, null, message, null);
    }
}
