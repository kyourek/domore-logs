using System;

namespace Domore.Logs {
    public sealed class Logging {
        private readonly static Logging Instance = new Logging();

        private LogServiceManager Manager {
            get => _Manager ?? (_Manager = new LogServiceManager());
            set => _Manager = value;
        }
        private LogServiceManager _Manager;

        internal bool Log(Logger logger, LogSeverity severity) {
            return Manager.Log(severity, logger?.Type);
        }

        internal void Log(Logger logger, LogSeverity severity, params object[] data) {
            Manager.Log(severity, logger?.Type, data);
        }

        public static object Config =>
            new { Log = Instance.Manager };

        public static ILog For(Type type) {
            return new Logger(type, Instance);
        }

        public static void Complete() {
            using (var manager = Instance.Manager) {
                manager.Complete();
            }
        }

        public static void Reset() {
            Instance.Manager = null;
        }
    }
}
