using System;
using DragonLib;
using JetBrains.Annotations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Logging;

[PublicAPI]
public class NullLogger : Singleton<NullLogger>, ILogger {
    public void Log(LogLevel level, string? category, string message, Exception? exception) { }
}
