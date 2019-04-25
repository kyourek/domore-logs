using System.Diagnostics;

namespace Domore.Logs.Handlers {
    class LogEvent : LogQueue.Handler {
#if NET40 || NET45
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

        public override void HandleWork(string message, LogSeverity severity) {
            using (var eventLog = new EventLog(LogName, MachineName, Source)) {
                eventLog.WriteEntry(message, type:
                    severity <= LogSeverity.Info ? EventLogEntryType.Information :
                    severity <= LogSeverity.Warn ? EventLogEntryType.Warning :
                    EventLogEntryType.Error);
            }
        }
#else
        public override void HandleWork(string message, LogSeverity severity) {
        }        
#endif
    }
}
