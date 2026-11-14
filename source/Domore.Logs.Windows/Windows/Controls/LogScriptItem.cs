using System.Runtime.InteropServices;

namespace Domore.Windows.Controls;

/*
 * Native SCRIPT_ITEM: a 32-bit position followed by the two 16-bit words of
 * SCRIPT_ANALYSIS. The low five bits of State contain the embedding level.
 */
[StructLayout(LayoutKind.Sequential)]
internal struct LogScriptItem {
    internal int Position;
    internal ushort Analysis;
    internal ushort State;
}
