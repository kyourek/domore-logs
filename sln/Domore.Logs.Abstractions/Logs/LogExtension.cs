using Microsoft.Extensions.Logging;

namespace Domore.Logs {
    using Abstractions;

    public static class LogExtension {
        public static ILoggerProvider Provider(this ILog log) =>
            new LoggerProviderImplementation { Log = log };
    }
}
