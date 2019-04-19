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
        bool DebugEnabled { get; }

        [DispId(2)]
        bool InfoEnabled { get; }

        [DispId(3)]
        bool WarnEnabled { get; }

        [DispId(4)]
        bool ErrorEnabled { get; }

        [DispId(5)]
        bool CriticalEnabled { get; }

        [DispId(6)]
        bool Enabled(LogSeverity severity);

        [DispId(7)]
        void Entry(LogSeverity severity, string message);

        [DispId(8)]
        void Debug(string message);

        [DispId(9)]
        void Info(string message);

        [DispId(10)]
        void Warn(string message);

        [DispId(11)]
        void Error(string message);

        [DispId(12)]
        void Critical(string message);

        [ComVisible(false)]
        void Entry(LogSeverity severity, params object[] data);

        [ComVisible(false)]
        void Debug(params object[] data);

        [ComVisible(false)]
        void Info(params object[] data);

        [ComVisible(false)]
        void Warn(params object[] data);

        [ComVisible(false)]
        void Error(params object[] data);

        [ComVisible(false)]
        void Critical(params object[] data);
    }
}
