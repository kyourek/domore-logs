using Microsoft.Extensions.Logging;

namespace Domore.Logs.Abstractions {
    internal static class LogLevelExtension {
        public static LogSeverity Severity(this LogLevel logLevel) {
            switch (logLevel) {
                case LogLevel.Critical: return LogSeverity.Critical;
                case LogLevel.Debug: return LogSeverity.Debug;
                case LogLevel.Error: return LogSeverity.Error;
                case LogLevel.Information: return LogSeverity.Info;
                case LogLevel.None: return LogSeverity.None;
                case LogLevel.Trace: return LogSeverity.Debug;
                case LogLevel.Warning: return LogSeverity.Warn;
                default: return LogSeverity.Debug;
            }
        }
    }
}
