using Domore.Logs;
using System;

namespace Domore.Windows.Controls;

/*
 * Refers to one display line of an entry without building its text. The text
 * is written only when the line is drawn.
 */
internal readonly record struct LogLine {
    public ILogEntry Entry { get; }
    public string Message { get; }
    public int Start { get; }
    public int Length { get; }
    public LogLineKind Kind { get; }

    public LogSeverity Severity => Entry.LogSeverity;

    public LogLine(ILogEntry entry, string message, int start, int length, LogLineKind kind) {
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        Message = message ?? string.Empty;
        Start = start;
        Length = length;
        Kind = kind;
    }
}
