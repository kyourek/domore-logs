using System.Runtime.InteropServices;

namespace Domore.Logs {
    [Guid("59872D41-4E8E-4361-B71B-FCEE0395568E")]
    [ComVisible(true)]
#if NETCOREAPP
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
#else
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    public interface ILogConfiguration {
        [ComVisible(false)]
        void Configure(ILogHandler handler, string key = null);
    }
}
