namespace Domore.Logs {
    using Configuration;

    class ConfigurationLogProvider {
        IConfigurationContainer _Container;
        public IConfigurationContainer Container {
            get => _Container ?? (_Container = new ConfigurationContainer());
            set => _Container = value;
        }
    }
}
