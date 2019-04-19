using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Domore.Logs {
    using ComponentModel;

    internal class Logger : NotifyPropertyChangedImplementation, ILog {
        private readonly object Locker = new object();

        private ISet<ILogHandler> Handlers { get { return _Handlers; } }
        private readonly ISet<ILogHandler> _Handlers = new HashSet<ILogHandler>();

        private void Handler_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e != null) {
                if (e.PropertyName == "Severity") {
                    var handler = sender as ILogHandler;
                    if (handler != null) {
                        var handlerSeverity = handler.Severity;
                        if (handlerSeverity < Severity) {
                            Severity = handlerSeverity;
                        }
                    }
                }
            }
        }

        private void Entry(LogEntry entry) {
            if (null == entry) throw new ArgumentNullException("entry");

            var handlers = GetHandlers();
            var dataDict = new Dictionary<string, string>();

            foreach (var handler in handlers) {
                if (handler.Severity <= entry.Severity) {
                    var fmt = handler.Format ?? "";
                    var data = default(string);
                    if (dataDict.TryGetValue(fmt, out data) == false) {
                        data = entry.ToString(fmt);
                        dataDict[fmt] = data;
                    }

                    handler.Handle(data);
                }
            }
        }

        public string Name { get { return _Name; } }
        private readonly string _Name;

        public Type Type { get { return _Type; } }
        private readonly Type _Type;

        public object Owner { get { return _Owner; } }
        private readonly object _Owner;

        public LogSeverity Severity {
            get { return _Severity; }
            private set {
                if (_Severity != value) {
                    _Severity = value;
                    NotifyPropertyChanged("Severity");
                }
            }
        }
        private LogSeverity _Severity = LogSeverity.None;

        public bool Enabled(LogSeverity severity) {
            return severity == LogSeverity.None
                ? false
                : severity >= Severity;
        }

        public bool AddHandler(ILogHandler handler) {
            if (null == handler) throw new ArgumentNullException("handler");
            
            var added = default(bool);
            lock (Locker) {
                added = Handlers.Add(handler);
            }

            if (added) {
                var propChanger = handler as INotifyPropertyChanged;
                if (propChanger != null) {
                    propChanger.PropertyChanged += Handler_PropertyChanged;
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
                var propChanger = handler as INotifyPropertyChanged;
                if (propChanger != null) {
                    propChanger.PropertyChanged -= Handler_PropertyChanged;
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

        public bool DebugEnabled {
            get { return Enabled(LogSeverity.Debug); }
        }

        public bool InfoEnabled {
            get { return Enabled(LogSeverity.Info); }
        }

        public bool WarnEnabled {
            get { return Enabled(LogSeverity.Warn); }
        }

        public bool ErrorEnabled {
            get { return Enabled(LogSeverity.Error); }
        }

        public bool CriticalEnabled {
            get { return Enabled(LogSeverity.Critical); }
        }

        public Logger(string name, Type type, object owner) {
            _Name = name;
            _Type = type;
            _Owner = owner;
        }

        public void Debug(params object[] data) {
            Entry(LogSeverity.Debug, data);
        }

        public void Info(params object[] data) {
            Entry(LogSeverity.Info, data);
        }

        public void Warn(params object[] data) {
            Entry(LogSeverity.Warn, data);
        }

        public void Error(params object[] data) {
            Entry(LogSeverity.Error, data);
        }

        public void Critical(params object[] data) {
            Entry(LogSeverity.Critical, data);
        }

        void ILog.Entry(LogSeverity severity, string message) {
            Entry(severity, message);
        }

        void ILog.Debug(string message) {
            Debug(message);
        }

        void ILog.Info(string message) {
            Info(message);
        }

        void ILog.Warn(string message) {
            Warn(message);
        }

        void ILog.Error(string message ){
            Error(message);
        }

        void ILog.Critical(string message) {
            Critical(message);
        }
    }
}
