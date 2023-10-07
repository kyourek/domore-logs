#if !NETFRAMEWORK
using Domore.Logs.Abstractions;
using Microsoft.Extensions.Logging;

namespace Domore.Logs {
    public static class LogExtension {
        public static ILoggerProvider Provider(this ILog log) =>
            new LoggerProviderImplementation { Log = log };
    }
}
#endif