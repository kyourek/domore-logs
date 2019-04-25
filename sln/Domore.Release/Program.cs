namespace Domore {
    using Configuration;

    class Program {
        static void Main(string[] args) {
            ConfigurationDefault.ContentsProvider = new AppSettingsProvider();
            new Release(args);
        }
    }
}