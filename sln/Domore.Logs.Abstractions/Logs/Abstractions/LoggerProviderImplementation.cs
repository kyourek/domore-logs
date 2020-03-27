using Microsoft.Extensions.Logging;
using System;

namespace Domore.Logs.Abstractions {
    public class LoggerProviderImplementation : ILoggerProvider {
        protected virtual void Dispose(bool disposing) {
        }

        public ILog Log { get; set; }

        public ILogger CreateLogger(string categoryName) =>
            new LoggerImplementation(categoryName) { Log = Log };

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~LoggerProviderImplementation() {
            Dispose(false);
        }
    }
}
