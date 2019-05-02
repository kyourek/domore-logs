namespace Domore.Conf {
    using Logs;

    public static class ConfContainerExtension {
        public static void ConfigureLogging(this IConfContainer confContainer) {
            Logging.Configuration = new LogConf { Container = confContainer };
        }
    }
}
