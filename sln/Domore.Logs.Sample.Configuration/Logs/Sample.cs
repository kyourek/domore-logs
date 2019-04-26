﻿using System;

namespace Domore.Logs.Sample {
    using Configuration;

    class Program {
        static void Main(string[] args) {
            ConfigurationDefault.Container.ContentsProvider = new AppSettingsProvider();
            ConfigurationDefault.Container.ConfigureLogging();

            new Sample().Run();
            Logging.Complete();

            Console.WriteLine("[Enter] to exit");
            Console.ReadLine();
        }
    }

    class Sample {
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