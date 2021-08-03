using System;
using DragonLib;
using Equilibrium.Interfaces;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Logging {
    [PublicAPI]
    public class NullLogger : Singleton<NullLogger>, ILogger {
        public void Log(LogLevel level, string? category, string message, Exception? exception) { }
    }
}
