using Domore.Logs;
using System;

namespace Domore.Windows.Controls;

/*
 * Refers to one display line of an entry without building its text. The text
 * is written only when the line is drawn. A line keeps the fields its header
 * needs rather than the entry itself, so a line that remains buffered does not
 * keep the entry's other messages alive.
 */
internal readonly record struct LogLine {
    public DateTime Date { get; }
    public LogSeverity Severity { get; }
    public string Name { get; }
    public string Message { get; }
    public int Start { get; }
    public int Length { get; }
    public LogLineKind Kind { get; }

    public LogLine(DateTime date, LogSeverity severity, string name, string message, int start, int length, LogLineKind kind) {
        Date = date;
        Severity = severity;
        Name = name ?? string.Empty;
        Message = message ?? string.Empty;
        Start = start;
        Length = length;
        Kind = kind;
    }
}
