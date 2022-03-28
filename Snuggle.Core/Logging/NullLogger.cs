using System;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Logging;

public sealed class NullLogger : Singleton<NullLogger>, ILogger {
    public void Log(LogLevel level, string category, string message, Exception? exception) { }

    public void Dispose() { }
}
