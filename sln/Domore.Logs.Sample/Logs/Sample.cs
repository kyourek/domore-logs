using System;

namespace Domore.Logs.Sample {
    class Program {
        static readonly ConsoleHandler CalculatorLog = new ConsoleHandler();

        static void Main(string[] args) {
            Logging.Add(CalculatorLog, typeof(Calculator));

            new Sample().Run();
            Logging.Complete();

            Console.WriteLine("[Enter] to exit");
            Console.ReadLine();
        }
    }

    class ConsoleHandler : ILogHandler {
        public LogSeverity Severity =>
            LogSeverity.Debug;

        public string Format =>
            "{Time:HH:mm:ss} {Name} [{Severity}] {Data}";

        public void Handle(string message, LogSeverity severity) =>
            Console.WriteLine(message);
    }

    class Sample {
        public void Run() {
            new Calculator().Add(1, 2);
        }
    }

    class Calculator {
        readonly ILog Log = Logging.For(typeof(Calculator));

        public void Add(int one, int two) {
            Log.Debug("Adding", one, two);
            Log.Info($"{one} + {two} = {one + two}");
            Log.Debug("Done");
        }
    }
}
