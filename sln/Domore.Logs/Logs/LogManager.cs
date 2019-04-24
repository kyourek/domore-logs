using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs {
    using Handlers;

    class LogManager {
        readonly object Locker = new object();
        readonly ICollection<WeakReference> LoggerReferences = new List<WeakReference>();
        readonly ICollection<WeakReference> HandlerReferences = new List<WeakReference>();
        readonly IDictionary<WeakReference, object[]> HandlerLogs = new Dictionary<WeakReference, object[]>();

        void AddHandlers(Logger logger) {
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
                            Owner = log as object })
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

        Logger CreateLogger(string name, Type type, object owner) {
            var config = Configuration;
            var logger = new Logger(name, type, owner);
            var logFile = new LogFile { Name = logger.Name + ".log", Severity = LogSeverity.Error };
            config.Configure(logFile);
            config.Configure(logFile, logFile.Name);
            logger.AddHandler(logFile);

            lock (Locker) {
                LoggerReferences.Add(new WeakReference(logger));
                AddHandlers(logger);
            }

            return logger;
        }

        ILogConfiguration _Configuration;
        public ILogConfiguration Configuration {
            get => _Configuration ?? (_Configuration = new LogConfiguration());
            set => _Configuration = value;
        }

        public ILog GetLog(string name, Type type, object owner) => CreateLogger(name, type, owner);

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
    }
}
