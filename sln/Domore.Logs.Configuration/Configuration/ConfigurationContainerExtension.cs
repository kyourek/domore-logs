namespace Domore.Configuration {
    using Logs;

    public static class ConfigurationContainerExtension {
        public static void ConfigureLogging(this IConfigurationContainer configurationContainer) {
            Logging.Configuration = new LogConfiguration { Container = configurationContainer };
        }
    }
}
