namespace Domore.Logs.Sample {
    using Configuration;

    class Sample {
        public void Run() {
            ConfigurationDefault.Container.ContentsProvider = new AppSettingsProvider();
            ConfigurationDefault.Container.ConfigureLogging();

            var log = Logging.For(this);
            log.Debug("Debug message written to file");
            log.Info("Info message written to file.");
            log.Warn("Warn message written to file.");
            log.Error("Error message written to file.");
            log.Critical("Critical message written to file.");
        }
    }
}
