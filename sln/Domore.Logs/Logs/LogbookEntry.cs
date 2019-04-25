namespace Domore.Logs {
    public class LogbookEntry {
        public string Message { get; }
        public LogSeverity Severity { get; }

        public LogbookEntry(string message, LogSeverity severity) {
            Message = message;
            Severity = severity;
        }
    }
}
