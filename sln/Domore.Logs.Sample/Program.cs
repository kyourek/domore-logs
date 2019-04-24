using System;

namespace Domore {
    using Logs;
    using Logs.Sample;

    class Program {
        static readonly ConsoleLog CalculatorLog = new ConsoleLog();

        static void Main(string[] args) {
            Logging.Add(CalculatorLog, typeof(Calculator));

            new Sample().Run();
            Console.WriteLine("[Enter] to exit");
            Console.ReadLine();
        }

        class ConsoleLog : ILogHandler {
            public LogSeverity Severity => LogSeverity.Debug;
            public string Format => null;
            public void Handle(string entry) => Console.WriteLine(entry);
        }
    }
}
