using Microsoft.Extensions.Logging;
using System;

namespace Domore.Logs.Abstractions {
    internal class LoggerProviderImplementation : ILoggerProvider {
        protected virtual void Dispose(bool disposing) {
        }

        public ILog Log { get; }

        public LoggerProviderImplementation(ILog log) =>
            Log = log;

        public ILogger CreateLogger(string categoryName) =>
            new LoggerImplementation(Log, categoryName);

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~LoggerProviderImplementation() {
            Dispose(false);
        }
    }
}
