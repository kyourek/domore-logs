using System;
using System.Runtime.InteropServices;

namespace Domore.Logs {
    using Handlers;

    [Guid("D52278CA-BF09-4640-8C39-8F4B057766E9")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class Logging : ILogging {
        private static readonly LogManager Manager = new LogManager();

        private static Logger For(string name, Type type, object owner) {
            //var config = Configuration.Block;
            var logger = /*config.Configure(*/new Logger(name, type, owner);

            var logFile = new LogFile { Name = logger.Name + ".log", Severity = LogSeverity.Error };
            //config.Configure(logFile);
            //config.Configure(logFile, logFile.Name);
            logger.AddHandler(logFile);

            var logMail = new LogMail { Name = logger.Name + ".mail" , Severity = LogSeverity.Critical };
            //config.Configure(logMail);
            //config.Configure(logMail, logMail.Name);
            logger.AddHandler(logMail);

            Manager.AddLogger(logger);

            return logger;
        }

        public static void Add(ILogHandler handler, params object[] logs) {
            Manager.AddHandler(handler, logs);
        }

        public static ILog For(Type type, string name = null) {
            if (null == type) throw new ArgumentNullException("type");
            return For(name ?? type.FullName, type, null);
        }

        public static ILog For(object owner, string name = null) {
            if (null == owner) throw new ArgumentNullException("owner");
            var type = owner.GetType();
            return For(name ?? type.FullName, type, owner);
        }

        ILog ILogging.For(string name) {
            return For(name, null, null);
        }
    }
}
