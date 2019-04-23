using System;
using System.Runtime.InteropServices;

namespace Domore.Logs {
    [Guid("D52278CA-BF09-4640-8C39-8F4B057766E9")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class Logging : ILogging {
        static readonly ILogProvider Provider = new LogProvider();

        static ILog For(string name, Type type, object owner) {
            return Provider.GetLog(name, type, owner);
        }

        public static void Add(ILogHandler handler, params object[] logs) {
            Provider.AddHandler(handler, logs);
        }

        public static ILog For(Type type, string name = null) {
            if (null == type) throw new ArgumentNullException(nameof(type));
            return For(name ?? type.FullName, type, null);
        }

        public static ILog For(object owner, string name = null) {
            if (null == owner) throw new ArgumentNullException(nameof(owner));
            var type = owner.GetType();
            return For(name ?? type.FullName, type, owner);
        }

        ILog ILogging.For(string name) {
            return For(name, null, null);
        }
    }
}
