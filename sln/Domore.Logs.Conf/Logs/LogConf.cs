namespace Domore.Logs {
    using Conf;

    internal class LogConf : ILogConfiguration {
        public IConfContainer Container {
            get => _Container ?? (_Container = Conf.Container);
            set => _Container = value;
        }
        private IConfContainer _Container;

        public void Configure(object obj, string key) {
            Container.Configure(obj, key);
        }
    }
}
