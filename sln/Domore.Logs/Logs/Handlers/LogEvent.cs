using System;
using System.Diagnostics;
using System.Linq;

namespace Domore.Logs.Handlers {
#if NETFRAMEWORK
    class LogEvent : LogQueue.Handler {
        static readonly string DefaultLogName;
        static readonly string DefaultMachineName;
        static readonly string DefaultSource;

        static LogEvent() {
            using (var eventLog = new EventLog()) {
                DefaultLogName = eventLog.Log;
                DefaultMachineName = eventLog.MachineName;
                DefaultSource = eventLog.Source;
            }
        }

        T Use<T>(Func<EventLog, T> action) {
            if (null == action) throw new ArgumentNullException(nameof(action));
            using (var eventLog = new EventLog(LogName, MachineName, Source)) {
                return action(eventLog);
            }
        }

        void Use(Action<EventLog> action) {
            if (null == action) throw new ArgumentNullException(nameof(action));
            Use(eventLog => {
                action(eventLog);
                return default(object);
            });
        }

        string _LogName;
        public string LogName {
            get => _LogName ?? (_LogName = DefaultLogName);
            set => Change(ref _LogName, value, nameof(LogName));
        }

        string _MachineName;
        public string MachineName {
            get => _MachineName ?? (_MachineName = DefaultMachineName);
            set => Change(ref _MachineName, value, nameof(MachineName));
        }

        string _Source;
        public string Source {
            get => _Source ?? (_Source = DefaultSource);
            set => Change(ref _Source, value, nameof(Source));
        }

        public string Read() {
            return Use(eventLog => {
                return eventLog.Entries.Cast<EventLogEntry>().Select(entry => entry.Message).Aggregate((s1, s2) => s1 + Environment.NewLine + s2);
            });
        }

        public override void HandleWork(string message, LogSeverity severity) {
            Use(eventLog => {
                eventLog.WriteEntry(message, type:
                    severity <= LogSeverity.Info ? EventLogEntryType.Information :
                    severity <= LogSeverity.Warn ? EventLogEntryType.Warning :
                    EventLogEntryType.Error);
            });
        }
    }
#endif
}
