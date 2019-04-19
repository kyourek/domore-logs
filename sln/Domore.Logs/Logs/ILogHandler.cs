using System.Runtime.InteropServices;

namespace Domore.Logs {
    [Guid("D0B7825D-C0F1-457A-A9B9-A147A77D8982")]
    [ComVisible(true)]
#if NETCOREAPP
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
#else
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    public interface ILogHandler {
    [DispId(1)]
        LogSeverity Severity { get; }

        [DispId(2)]
        string Format { get; }

        [DispId(3)]
        void Handle(string entry);
    }
}
