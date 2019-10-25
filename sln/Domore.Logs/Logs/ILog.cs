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
        bool Entry(LogSeverity severity, object message);

        [DispId(2)]
        bool Debug(object message);

        [DispId(3)]
        bool Info(object message);

        [DispId(4)]
        bool Warn(object message);

        [DispId(5)]
        bool Error(object message);

        [DispId(6)]
        bool Critical(object message);

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
