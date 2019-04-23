using System;

namespace Domore.Logs {
    interface ILogProvider {
        ILog GetLog(string name, Type type, object owner);
        void AddHandler(ILogHandler handler, params object[] logs);
    }
}
