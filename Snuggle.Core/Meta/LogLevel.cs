using JetBrains.Annotations;

namespace Snuggle.Core.Meta; 

[PublicAPI]
public enum LogLevel {
    Debug,
    Info,
    Warning,
    Error,
    Crash,
}