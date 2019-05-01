using System;
using System.Collections.Generic;
using System.Linq;

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
            internal
            set => Change(ref _Name, value, nameof(Name));
        }

        string _Format;
        public string Format {
            get => _Format;
            set => Change(ref _Format, value, nameof(Format));
        }

        public abstract void Handle(string message, LogSeverity severity);

        internal class Factory {
            IList<Type> _HandlerTypes;
            IList<Type> HandlerTypes {
                get => _HandlerTypes ?? (_HandlerTypes = typeof(LogHandler).Assembly.GetTypes()
                    .Where(t => typeof(LogHandler).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToList());
            }

            LogHandler CreateHandler(string kind) {
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
