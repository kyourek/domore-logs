using System.Runtime.InteropServices;

namespace Domore.Logs {
    [Guid("0DA1DB3C-5C5D-4F2D-94FB-A5111B1A5CA7")]
    [ComVisible(true)]
    public enum LogSeverity {
        Debug = 1,
        Info,
        Warn,
        Error,
        Critical,
        None = 99
    }
}
