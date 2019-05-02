namespace Domore.Logs {
    using Conf;

    class LogConf : ILogConfiguration {
        IConfContainer _Container;
        public IConfContainer Container {
            get => _Container ?? (_Container = Conf.Container);
            set => _Container = value;
        }

        public void Configure(object obj, string key) {
            Container.Configure(obj, key);
        }
    }
}
