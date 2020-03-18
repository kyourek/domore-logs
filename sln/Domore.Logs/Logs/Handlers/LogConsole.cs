using System;

namespace Domore.Logs.Handlers {
    internal class LogConsole : LogHandler {
        public override void Handle(string message, LogSeverity severity) {
            Console.WriteLine(message);
        }
    }
}
