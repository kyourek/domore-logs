using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Domore.Logs {
    [Guid("9EF02708-4A4D-4FB9-9F2A-6FC6B0A611B9")]
    [ComVisible(true)]
#if NETCOREAPP
    [ClassInterface(ClassInterfaceType.None)]
#else
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
#endif
    public class LogEntry {
        public string LogName { get; }
        public DateTime Time { get; }
        public LogSeverity Severity { get; }
        public object[] Data { get; }
        public string[] DataStrings { get; }

        string _DataString;
        public string DataString {
            get => _DataString ?? (_DataString = string.Join(Environment.NewLine, DataStrings));
        }

        public LogEntry(DateTime time, LogSeverity severity, string logName, object[] data) {
            LogName = logName;
            Severity = severity;
            Time = time;
            Data = data;
            DataStrings = (Data ?? new object[] { })
                .Where(d => d != null)
                .Select(d => (d.ToString() ?? "").Trim())
                .ToArray();
        }

        public LogEntry(DateTime time, LogSeverity severity, object[] data) : this(time, severity, null, data) { }
        public LogEntry(LogSeverity severity, string logName, object[] data) : this(DateTime.Now, severity, logName, data) { }
        public LogEntry(LogSeverity severity, object[] data) : this(severity, null, data) { }

        public string ToString(string format) {
            format = (format ?? "").Trim();
            format = (format == "") ? "{Time:yyyy-MM-dd HH:mm:ss} [{Severity}] {Data}" : format;
            format = (format
                .Replace("Name", "0")
                .Replace("Time", "1")
                .Replace("Severity", "2")
                .Replace("Data", "3"));
            
            return string.Format(format,
                LogName,
                Time,
                Severity,
                DataString);
        }

        public override string ToString() {
            return ToString(null);
        }

        public static LogEntry Create(string message, Exception error = null, LogSeverity? severity = null) {
            if (severity == null) {
                severity = error == null
                    ? LogSeverity.Info
                    : LogSeverity.Error;
            }

            var data = error == null
                ? new object[] { message }
                : new object[] { message, error };

            return new LogEntry(severity.Value, data);
        }
    }
}
