using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs {
    internal class LogManager : IDisposable {
        private readonly object Locker = new object();
        private readonly ICollection<WeakReference> LoggerReferences = new List<WeakReference>();
        private readonly ICollection<WeakReference> HandlerReferences = new List<WeakReference>();
        private readonly IDictionary<WeakReference, object[]> HandlerLogs = new Dictionary<WeakReference, object[]>();

        private LogQueue Queue {
            get {
                if (_Queue == null) {
                    lock (Locker) {
                        if (_Queue == null) {
                            _Queue = new LogQueue();
                        }
                    }
                }
                return _Queue;
            }
        }
        private LogQueue _Queue;

        private LogHandler.Factory HandlerFactory =>
            _HandlerFactory ?? (
            _HandlerFactory = new LogHandler.Factory());
        private LogHandler.Factory _HandlerFactory;

        private void AddHandlers(Logger logger) {
            if (null == logger) throw new ArgumentNullException(nameof(logger));
            var references = HandlerReferences.ToList();
            foreach (var reference in references) {
                var handler = reference.Target as ILogHandler;
                if (handler == null) {
                    HandlerReferences.Remove(reference);
                    HandlerLogs.Remove(reference);
                }
                else {
                    var add = logger.ContainsHandler(handler) == false && HandlerLogs[reference]
                        .Select(log => new {
                            Name = log as string,
                            Type = log as Type,
                            Owner = log as object
                        })
                        .Any(log =>
                            (log.Name != null && log.Name.Equals(logger.Name)) ||
                            (log.Type != null && log.Type.IsAssignableFrom(logger.Type)) ||
                            (log.Owner != null && log.Owner.Equals(logger.Owner)));

                    if (add) {
                        logger.AddHandler(handler);
                    }
                }
            }
        }

        private Logger CreateLogger(string name, Type type, object owner) {
            var config = Configuration;
            var logger = new Logger(name, type, owner);
            foreach (var item in Handler) {
                var handler = HandlerFactory.CreateHandler(item.Value, $"{logger.Name}.{item.Key}");
                if (handler is LogQueue.Handler handlerInQueue) {
                    handlerInQueue.Queue = Queue;
                }
                config.Configure(handler);
                config.Configure(handler, handler.Name);
                logger.AddHandler(handler);
            }

            lock (Locker) {
                LoggerReferences.Add(new WeakReference(logger));
                AddHandlers(logger);
            }

            return logger;
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (_Queue != null) {
                    lock (Locker) {
                        if (_Queue != null) {
                            _Queue.Dispose();
                        }
                    }
                }
            }
        }

        public IDictionary<string, string> Handler {
            get => _Handler ?? (_Handler = new Dictionary<string, string>());
            set => _Handler = value;
        }
        private IDictionary<string, string> _Handler;

        public ILogConfiguration Configuration {
            get => _Configuration ?? (_Configuration = new LogConfiguration());
            set => _Configuration = value;
        }
        private ILogConfiguration _Configuration;

        public TimeSpan CompleteTimeout {
            get => _CompleteTimeout;
            set => _CompleteTimeout = value;
        }
        private TimeSpan _CompleteTimeout = TimeSpan.FromSeconds(5);

        public ILog GetLog(string name, Type type, object owner) =>
            CreateLogger(name, type, owner);

        public void AddHandler(ILogHandler handler, params object[] logs) {
            lock (Locker) {
                var handlerReference = new WeakReference(handler);
                HandlerLogs[handlerReference] = logs;
                HandlerReferences.Add(handlerReference);

                var references = LoggerReferences.ToList();
                foreach (var reference in references) {
                    var logger = reference.Target as Logger;
                    if (logger == null) {
                        LoggerReferences.Remove(reference);
                    }
                    else {
                        AddHandlers(logger);
                    }
                }
            }
        }

        public void Complete() {
            Queue.Complete(CompleteTimeout);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~LogManager() {
            Dispose(false);
        }

        private class HandlerCollection {
            private readonly IDictionary<string, string> Dictionary = new Dictionary<string, string>();

            public string this[string key] {
                set => Dictionary[key] = value;
            }
        }
    }
}
