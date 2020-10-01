using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Domore.Logs {
    using Notification;

    internal class Logger : Notifier, ILog {
        private readonly ISet<ILogHandler> Handlers = new HashSet<ILogHandler>();

        private void Handler_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e != null) {
                if (e.PropertyName == nameof(ILogHandler.Severity)) {
                    if (sender is ILogHandler handler) {
                        var handlerSeverity = handler.Severity;
                        if (handlerSeverity < Severity) {
                            Severity = handlerSeverity;
                        }
                    }
                }
            }
        }

        private bool Enter(LogSeverity severity, object[] data) {
            var noData = data == null || data.Length == 0;
            if (noData) return severity == LogSeverity.None 
                ? false 
                : (severity >= Severity);

            var entry = new LogEntry(severity, Name, data);
            var handled = false;
            var handlers = GetHandlers();
            var messages = new Dictionary<string, string>();

            foreach (var handler in handlers) {
                if (handler.Severity <= severity) {
                    var format = handler.Format ?? "";

                    if (messages.TryGetValue(format, out var message) == false) {
                        messages[format] = message = entry.ToString(format);
                    }

                    handler.Handle(message, severity);
                    handled = true;
                }
            }

            return handled;
        }

        public Type Type { get; }
        public string Name { get; }
        public object Owner { get; }

        public LogSeverity Severity {
            get => _Severity;
            private set => Change(ref _Severity, value, nameof(Severity));
        }
        private LogSeverity _Severity = LogSeverity.None;

        public bool AddHandler(ILogHandler handler) {
            if (null == handler) throw new ArgumentNullException(nameof(handler));

            var added = default(bool);
            lock (Handlers) {
                added = Handlers.Add(handler);
            }

            if (added) {
                if (handler is INotifyPropertyChanged implementation) {
                    implementation.PropertyChanged += Handler_PropertyChanged;
                }

                var handlerSeverity = handler.Severity;
                if (handlerSeverity < Severity) {
                    Severity = handlerSeverity;
                }
            }

            return added;
        }

        public bool RemoveHandler(ILogHandler handler) {
            var removed = default(bool);
            lock (Handlers) {
                removed = Handlers.Remove(handler);
            }

            if (removed) {
                if (handler is INotifyPropertyChanged implementation) {
                    implementation.PropertyChanged -= Handler_PropertyChanged;
                }

                var handlerSeverity = handler.Severity;
                if (handlerSeverity == Severity) {
                    var handlers = GetHandlers();
                    Severity = handlers.Any()
                        ? handlers.Min(h => h.Severity)
                        : LogSeverity.None;
                }
            }

            return removed;
        }

        public bool ContainsHandler(ILogHandler handler) {
            lock (Handlers) {
                return Handlers.Contains(handler);
            }
        }

        public IEnumerable<ILogHandler> GetHandlers() {
            lock (Handlers) {
                return Handlers.ToList();
            }
        }

        public Logger(string name, Type type, object owner) {
            Name = name;
            Type = type;
            Owner = owner;
        }

        public bool Entry(LogSeverity severity, params object[] data) => Enter(severity, data);
        public bool Debug(params object[] data) => Enter(LogSeverity.Debug, data);
        public bool Info(params object[] data) => Enter(LogSeverity.Info, data);
        public bool Warn(params object[] data) => Enter(LogSeverity.Warn, data);
        public bool Error(params object[] data) => Enter(LogSeverity.Error, data);
        public bool Critical(params object[] data) => Enter(LogSeverity.Critical, data);

        public bool Entry(LogSeverity severity, object message) => Enter(severity, message == null ? null : new[] { message });
        public bool Debug(object message) => Enter(LogSeverity.Debug, message == null ? null : new[] { message });
        public bool Info(object message) => Enter(LogSeverity.Info, message == null ? null : new[] { message });
        public bool Warn(object message) => Enter(LogSeverity.Warn, message == null ? null : new[] { message });
        public bool Error(object message) => Enter(LogSeverity.Error, message == null ? null : new[] { message });
        public bool Critical(object message) => Enter(LogSeverity.Critical, message == null ? null : new[] { message });
    }
}
