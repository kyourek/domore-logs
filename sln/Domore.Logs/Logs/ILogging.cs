using System.Runtime.InteropServices;

namespace Domore.Logs {
    [Guid("5D009A1B-0F1C-4C78-B741-54F10B3BAF06")]
    [ComVisible(true)]
#if NETCOREAPP
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
#else
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    public interface ILogging {
        [DispId(1)]
        ILog For(string name);
    }
}
