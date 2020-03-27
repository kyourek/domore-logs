using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Domore.Logs.Abstractions {
    internal class LoggerImplementation : ILogger {
        private readonly List<object> Headers;

        public string CategoryName { get; }

        public ILog Log {
            get => _Log ?? (_Log = Logging.For(typeof(ILogger)));
            set => _Log = value;
        }
        private ILog _Log;

        public LoggerImplementation(string categoryName) {
            CategoryName = categoryName;
            Headers = new List<object> { CategoryName };
        }

        IDisposable ILogger.BeginScope<TState>(TState state) =>
            new Scope(this, state);

        bool ILogger.IsEnabled(LogLevel logLevel) =>
            Log.Entry(logLevel.Severity());

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
            var list = new List<object>(Headers) { eventId };

            if (formatter == null) {
                if (state != null) list.Add(state);
                if (exception != null) list.Add(exception);
            }
            else {
                list.Add(formatter.Invoke(state, exception));
            }

            var data = list.ToArray();
            var severity = logLevel.Severity();
            Log.Entry(severity, data);
        }

        private class Scope : IDisposable {
            protected virtual void Dispose(bool disposing) {
                if (disposing) {
                    Logger.Headers.Reverse();
                    Logger.Headers.Remove(State);
                    Logger.Headers.Reverse();
                }
            }

            public object State { get; }
            public LoggerImplementation Logger { get; }

            public Scope(LoggerImplementation logger, object state) {
                State = state;
                Logger = logger ?? throw new ArgumentNullException(nameof(logger));
                Logger.Headers.Add(State);
            }

            public void Dispose() {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            ~Scope() {
                Dispose(false);
            }
        }
    }
}
