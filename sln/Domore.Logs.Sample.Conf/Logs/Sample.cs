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
            log.Debug("Debug message logged");
            log.Info("Info message logged");
            log.Warn("Warn message logged");
            log.Error("Error message logged");
            log.Critical("Critical message logged");
        }
    }
}
