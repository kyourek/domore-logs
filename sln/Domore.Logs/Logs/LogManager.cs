using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs {
    internal class LogManager {
        private readonly object Locker = new object();
        private readonly ICollection<WeakReference> LoggerReferences = new List<WeakReference>();
        private readonly ICollection<WeakReference> HandlerReferences = new List<WeakReference>();
        private readonly IDictionary<WeakReference, object[]> HandlerLogs = new Dictionary<WeakReference, object[]>();

        private void AddHandlers(Logger logger) {
            if (null == logger) throw new ArgumentNullException("logger");

            HandlerReferences
                .ToList()
                .ForEach(reference => {
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
                });
        }

        public void AddLogger(Logger logger) {
            lock (Locker) {
                LoggerReferences.Add(new WeakReference(logger));
                AddHandlers(logger);
            }
        }

        public void AddHandler(ILogHandler handler, params object[] logs) {
            lock (Locker) {
                var handlerReference = new WeakReference(handler);
                HandlerLogs[handlerReference] = logs;
                HandlerReferences.Add(handlerReference);

                LoggerReferences
                    .ToList()
                    .ForEach(reference => {
                        var logger = reference.Target as Logger;
                        if (logger == null) {
                            LoggerReferences.Remove(reference);
                        }
                        else {
                            AddHandlers(logger);
                        }
                    });
            }
        }
    }
}
