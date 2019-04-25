namespace Domore.Logs {
    using ComponentModel;

    public abstract class LogHandler : NotifyPropertyChangedImplementation, ILogHandler {
        LogSeverity _Severity = LogSeverity.None;
        public LogSeverity Severity {
            get => _Severity;
            set => Change(ref _Severity, value, nameof(Severity));
        }

        string _Name;
        public string Name {
            get => _Name;
            set => Change(ref _Name, value, nameof(Name));
        }

        string _Format;
        public string Format {
            get => _Format;
            set => Change(ref _Format, value, nameof(Format));
        }

        public abstract void Handle(string message, LogSeverity severity);
    }
}
