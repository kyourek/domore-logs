using System;

namespace Domore.Logs {
    using Handlers;

    class LogProvider : ILogProvider {
        readonly LogManager Manager = new LogManager();

        Logger CreateLog(string name, Type type, object owner) {
            //var config = Configuration.Block;
            var logger = /*config.Configure(*/new Logger(name, type, owner);

            var logFile = new LogFile { Name = logger.Name + ".log", Severity = LogSeverity.Error };
            //config.Configure(logFile);
            //config.Configure(logFile, logFile.Name);
            logger.AddHandler(logFile);

            var logMail = new LogMail { Name = logger.Name + ".mail", Severity = LogSeverity.Critical };
            //config.Configure(logMail);
            //config.Configure(logMail, logMail.Name);
            logger.AddHandler(logMail);

            Manager.AddLogger(logger);

            return logger;
        }

        public ILog GetLog(string name, Type type, object owner) {
            return CreateLog(name, type, owner);
        }

        public void AddHandler(ILogHandler handler, params object[] logs) {
            Manager.AddHandler(handler, logs);
        }
    }
}
