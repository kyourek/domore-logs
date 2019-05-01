using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Domore.Logs {
    [Guid("D52278CA-BF09-4640-8C39-8F4B057766E9")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class Logging : ILogging {
        static LogManager _Manager;
        static LogManager Manager {
            get {
                if (_Manager == null) {
                    _Manager = new LogManager { Configuration = Configuration, Handler = Handler };
                    Configuration.Configure(_Manager);
                    Configuration.Configure(_Manager, "Logging");
                }
                return _Manager;
            }
            set => _Manager = value;
        }

        static ILog For(string name, Type type, object owner) => 
            Manager.GetLog(name, type, owner);

        static IDictionary<string, string> _Handler;
        internal static IDictionary<string, string> Handler {
            get => _Handler ?? (_Handler = new Dictionary<string, string>());
        }

        static ILogConfiguration _Configuration;
        public static ILogConfiguration Configuration {
            get => _Configuration ?? (_Configuration = new LogConfiguration());
            set => _Configuration = value;
        }

        public static TimeSpan CompleteTimeout {
            get => Manager.CompleteTimeout;
            set => Manager.CompleteTimeout = value;
        }

        public static void Add(ILogHandler handler, params object[] logs) => 
            Manager.AddHandler(handler, logs);

        public static ILog For(Type type, string name = null) {
            if (null == type) throw new ArgumentNullException(nameof(type));
            return For(name ?? type.FullName, type, null);
        }

        public static ILog For(object owner, string name = null) {
            if (null == owner) throw new ArgumentNullException(nameof(owner));
            var type = owner.GetType();
            return For(name ?? type.FullName, type, owner);
        }

        public static void Complete() {
            Manager.Complete();
            Manager.Dispose();
        }

        public static void Reset() {
            Manager = null;
        }

        TimeSpan ILogging.CompleteTimeout { get => CompleteTimeout; set => CompleteTimeout = value; }
        ILogConfiguration ILogging.Configuration { get => Configuration; set => Configuration = value; }
        ILog ILogging.For(string name) => For(name, null, null);
        ILog ILogging.For(Type type, string name) => For(type, name);
        ILog ILogging.For(object owner, string name) => For(owner, name);
        void ILogging.Add(ILogHandler handler, object log) => Add(handler, log);
        void ILogging.Add(ILogHandler handler, params object[] logs) => Add(handler, logs);
        void ILogging.Complete() => Complete();
        void ILogging.Reset() => Reset();
    }
}
