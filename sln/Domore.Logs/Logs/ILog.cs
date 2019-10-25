using System.Runtime.InteropServices;

namespace Domore.Logs {
    [Guid("AA23D6E6-5744-410B-B30D-909106B8AA79")]
    [ComVisible(true)]
#if NETCOREAPP
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
#else
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    public interface ILog {
        [DispId(1)]
        bool Entry(LogSeverity severity, string message);

        [DispId(2)]
        bool Debug(string message);

        [DispId(3)]
        bool Info(string message);

        [DispId(4)]
        bool Warn(string message);

        [DispId(5)]
        bool Error(string message);

        [DispId(6)]
        bool Critical(string message);

        [ComVisible(false)]
        bool Entry(LogSeverity severity, params object[] data);

        [ComVisible(false)]
        bool Debug(params object[] data);

        [ComVisible(false)]
        bool Info(params object[] data);

        [ComVisible(false)]
        bool Warn(params object[] data);

        [ComVisible(false)]
        bool Error(params object[] data);

        [ComVisible(false)]
        bool Critical(params object[] data);
    }
}
