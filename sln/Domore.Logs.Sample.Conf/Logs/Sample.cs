using System;

namespace Domore.Logs.Sample {
    using Conf;

    internal class Program {
        private static void Main(string[] args) {
            Conf.Container.ContentsProvider = new AppSettingsProvider();
            Conf.Container.ConfigureLogging();

            new Sample().Run();
            Logging.Complete();

            Console.WriteLine("[Enter] to exit");
            Console.ReadLine();
        }
    }

    internal class Sample {
        public void Run() {
            var log = Logging.For(this);
            log.Debug("Debug message written to file");
            log.Info("Info message written to file.");
            log.Warn("Warn message written to file.");
            log.Error("Error message written to file.");
            log.Critical("Critical message written to file.");
        }
    }
}
