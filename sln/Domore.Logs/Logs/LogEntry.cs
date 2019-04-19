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
        public DateTime Time { get { return _Time; } }
        private readonly DateTime _Time;

        public LogSeverity Severity { get { return _Severity; } }
        private readonly LogSeverity _Severity;

        public object[] Data { get { return _Data; } }
        private readonly object[] _Data;

        public string[] DataStrings { get { return _DataStrings; } }
        private readonly string[] _DataStrings;

        public string DataString {
            get { return _DataString ?? (_DataString = string.Join(Environment.NewLine, DataStrings)); }
        }
        private string _DataString;

        public string LogName { get { return _LogName; } }
        private readonly string _LogName;

        public LogEntry(DateTime time, LogSeverity severity, string logName, object[] data) {
            _LogName = logName;
            _Severity = severity;
            _Time = time;
            _Data = data;
            _DataStrings = (_Data ?? new object[] { })
                .Where(datum => datum != null)
                .Select(datum => (datum.ToString() ?? "").Trim())
                .ToArray();
        }

        public LogEntry(DateTime time, LogSeverity severity, object[] data)
            : this(time, severity, null, data) { }

        public LogEntry(LogSeverity severity, string logName, object[] data) 
            : this(DateTime.Now, severity, logName, data) { }

        public LogEntry(LogSeverity severity, object[] data) 
            : this(severity, null, data) { }

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
