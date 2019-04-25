namespace Domore.Logs {
    using Configuration;

    class LogConfiguration : ILogConfiguration {
        public IConfigurationContainer Container { get; set; }

        public void Configure(object obj, string key) {
            var cont = Container ?? ConfigurationDefault.Container;
            var block = cont.Block;
            block.Configure(obj, key);
        }
    }
}
