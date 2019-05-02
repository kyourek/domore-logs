namespace Domore {
    using Conf;
    using Conf2 = Conf.Conf;

    static class Config {
        static Config() {
            Conf2.ContentsProvider = new AppSettingsProvider();
        }

        public static T Configure<T>(T obj, string key = null) {
            return Conf2.Configure(obj, key);
        }
    }
}
