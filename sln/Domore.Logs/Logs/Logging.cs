using System;
using System.Runtime.InteropServices;

namespace Domore.Logs {
    [Guid("D52278CA-BF09-4640-8C39-8F4B057766E9")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class Logging : ILogging {
        static readonly LogManager Manager = new LogManager();

        static ILog For(string name, Type type, object owner) => 
            Manager.GetLog(name, type, owner);

        public static ILogConfiguration Configuration {
            get => Manager.Configuration;
            set => Manager.Configuration = value;
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

        ILogConfiguration ILogging.Configuration {
            get => Configuration;
            set => Configuration = value;
        }

        ILog ILogging.For(string name) => For(name, null, null);
        ILog ILogging.For(Type type, string name) => For(type, name);
        ILog ILogging.For(object owner, string name) => For(owner, name);
        void ILogging.Add(ILogHandler handler, object log) => Add(handler, log);
        void ILogging.Add(ILogHandler handler, params object[] logs) => Add(handler, logs);
    }
}
