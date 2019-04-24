using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Domore.Logs {
    using ComponentModel;

    class Logger : NotifyPropertyChangedImplementation, ILog {
        readonly object Locker = new object();

        ISet<ILogHandler> Handlers { get; } = new HashSet<ILogHandler>();

        void Handler_PropertyChanged(object sender, PropertyChangedEventArgs e) {
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

        private void Entry(LogEntry entry) {
            if (null == entry) throw new ArgumentNullException(nameof(entry));

            var handlers = GetHandlers();
            var dataDict = new Dictionary<string, string>();

            foreach (var handler in handlers) {
                if (handler.Severity <= entry.Severity) {
                    var fmt = handler.Format ?? "";
                    if (dataDict.TryGetValue(fmt, out var data) == false) {
                        data = entry.ToString(fmt);
                        dataDict[fmt] = data;
                    }

                    handler.Handle(data);
                }
            }
        }

        public string Name { get; }
        public Type Type { get; }
        public object Owner { get; }

        LogSeverity _Severity = LogSeverity.None;
        public LogSeverity Severity {
            get => _Severity;
            private set => Change(ref _Severity, value, nameof(Severity));
        }

        public bool Enabled(LogSeverity severity) => severity == LogSeverity.None ? false : (severity >= Severity);

        public bool AddHandler(ILogHandler handler) {
            if (null == handler) throw new ArgumentNullException(nameof(handler));
            
            var added = default(bool);
            lock (Locker) {
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
            lock (Locker) {
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
            lock (Locker) {
                return Handlers.Contains(handler);
            }
        }

        public IEnumerable<ILogHandler> GetHandlers() {
            lock (Locker) {
                return Handlers.ToList();
            }
        }

        public void Entry(LogSeverity severity, params object[] data) {
            Entry(new LogEntry(severity, Name, data));
        }

        public bool DebugEnabled { get => Enabled(LogSeverity.Debug); }
        public bool InfoEnabled { get => Enabled(LogSeverity.Info); }
        public bool WarnEnabled { get => Enabled(LogSeverity.Warn); }
        public bool ErrorEnabled { get => Enabled(LogSeverity.Error); }
        public bool CriticalEnabled { get => Enabled(LogSeverity.Critical); }

        public Logger(string name, Type type, object owner) {
            Name = name;
            Type = type;
            Owner = owner;
        }

        public void Debug(params object[] data) => Entry(LogSeverity.Debug, data);
        public void Info(params object[] data) => Entry(LogSeverity.Info, data);
        public void Warn(params object[] data) => Entry(LogSeverity.Warn, data);
        public void Error(params object[] data) => Entry(LogSeverity.Error, data);
        public void Critical(params object[] data) => Entry(LogSeverity.Critical, data);

        void ILog.Entry(LogSeverity severity, string message) => Entry(severity, message);
        void ILog.Debug(string message) => Debug(message);
        void ILog.Info(string message) => Info(message);
        void ILog.Warn(string message) => Warn(message);
        void ILog.Error(string message) => Error(message);
        void ILog.Critical(string message) => Critical(message);
    }
}
