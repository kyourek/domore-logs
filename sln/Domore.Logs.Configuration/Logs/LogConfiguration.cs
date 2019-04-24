namespace Domore.Logs {
    using Configuration;

    class LogConfiguration : ILogConfiguration {
        public IConfigurationContainer Container { get; set; }

        public void Configure(ILogHandler handler, string key) {
            var cont = Container ?? ConfigurationDefault.Container;
            var block = cont.Block;
            block.Configure(handler, key);
        }
    }
}
