using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs {
    using Notification;

    public abstract class LogHandler : Notifier, ILogHandler {
        public LogSeverity Severity {
            get => _Severity;
            set => _Severity = Change(_Severity, value, nameof(Severity));
        }
        private LogSeverity _Severity = LogSeverity.None;

        public string Name {
            get => _Name;
            internal set => _Name = Change(_Name, value, nameof(Name));
        }
        private string _Name;

        public string Format {
            get => _Format;
            set => _Format = Change(_Format, value, nameof(Format));
        }
        private string _Format;

        public abstract void Handle(string message, LogSeverity severity);

        internal class Factory {
            private IList<Type> HandlerTypes =>
                _HandlerTypes ?? (
                _HandlerTypes = typeof(LogHandler).Assembly
                    .GetTypes()
                    .Where(t => typeof(LogHandler).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToList());
            private IList<Type> _HandlerTypes;

            private LogHandler CreateHandler(string kind) {
                if (null == kind) throw new ArgumentNullException(nameof(kind));

                string u(string s) => string.Join("", s.Trim().ToUpperInvariant().Split());
                var k = u(kind);
                foreach (var type in HandlerTypes) {
                    var t = u(type.Name);
                    if (t == k) return (LogHandler)Activator.CreateInstance(type);
                }

                throw new ArgumentException(paramName: nameof(kind), message: $"Invalid kind '{kind}'");
            }

            public LogHandler CreateHandler(string kind, string name) {
                var handler = CreateHandler(kind);
                handler.Name = name;
                return handler;
            }
        }
    }
}
