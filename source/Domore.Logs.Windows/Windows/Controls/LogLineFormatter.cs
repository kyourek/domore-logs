using Domore.Logs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Windows.Controls;

/*
 * Renders entries like the library's default text format: a single message
 * follows its header, and multiple messages are indented beneath it. Split
 * runs when entries arrive and only locates line breaks. Write builds a
 * line's text when the line is drawn.
 */
internal static class LogLineFormatter {
    private const int TabWidth = 4;
    private const int HeaderPadding = 32;

    private static string Abbreviate(LogSeverity severity) {
        return severity switch {
            LogSeverity.Critical => "crt",
            LogSeverity.Debug => "dbg",
            LogSeverity.Error => "err",
            LogSeverity.Info => "inf",
            LogSeverity.Warn => "wrn",
            _ => severity.ToString().ToLowerInvariant()
        };
    }

    private static void AddMessage(List<LogLine> lines, ILogEntry entry, string message, LogLineKind kind) {
        var text = message ?? string.Empty;
        var start = 0;
        var more = true;
        while (more) {
            var end = text.IndexOf('\n', start);
            more = end >= 0;
            var stop = more ? end : text.Length;
            var length = stop - start;
            bool endsWithReturn = length > 0 && text[stop - 1] == '\r';
            if (endsWithReturn) {
                length--;
            }
            lines.Add(new LogLine(entry, text, start, length, kind));
            start = stop + 1;
        }
    }

    private static int WriteDigits(char[] buffer, int position, int value, int digits) {
        for (var i = digits - 1; i >= 0; i--) {
            buffer[position + i] = (char)('0' + value % 10);
            value /= 10;
        }
        return position + digits;
    }

    private static int WriteText(char[] buffer, int position, string text) {
        text.CopyTo(0, buffer, position, text.Length);
        return position + text.Length;
    }

    private static int WriteHeader(char[] buffer, int position, ILogEntry entry) {
        var time = entry.LogDate.ToLocalTime();
        position = WriteDigits(buffer, position, time.Hour, 2);
        buffer[position++] = ':';
        position = WriteDigits(buffer, position, time.Minute, 2);
        buffer[position++] = ':';
        position = WriteDigits(buffer, position, time.Second, 2);
        buffer[position++] = '.';
        position = WriteDigits(buffer, position, time.Millisecond, 3);
        buffer[position++] = ' ';
        position = WriteText(buffer, position, Abbreviate(entry.LogSeverity));
        buffer[position++] = ' ';
        return WriteText(buffer, position, entry.LogName ?? string.Empty);
    }

    private static int WriteSegment(char[] buffer, int position, string text, int start, int length) {
        var stop = start + length;
        for (var i = start; i < stop; i++) {
            var c = text[i];
            bool isTab = c == '\t';
            if (isTab) {
                for (var j = 0; j < TabWidth; j++) {
                    buffer[position++] = ' ';
                }
                continue;
            }
            bool isControl = char.IsControl(c);
            buffer[position++] = isControl ? ' ' : c;
        }
        return position;
    }

    public static void Split(ILogEntry entry, List<LogLine> lines) {
        var messages = entry.LogList as IList<string> ?? entry.LogList?.ToArray() ?? [];
        bool single = messages.Count == 1;
        if (single) {
            AddMessage(lines, entry, messages[0], LogLineKind.Headed);
            return;
        }
        lines.Add(new LogLine(entry, null, 0, 0, LogLineKind.Header));
        for (var i = 0; i < messages.Count; i++) {
            AddMessage(lines, entry, messages[i], LogLineKind.Indented);
        }
    }

    /*
     * Collect newest-first so a multiline burst only visits lines that can
     * survive the history limit. The caller reverses the completed batch once.
     */
    public static void SplitNewest(ILogEntry entry, List<LogLine> lines, int limit) {
        var messages = entry.LogList as IList<string> ?? entry.LogList?.ToArray() ?? [];
        var kind = messages.Count == 1 ? LogLineKind.Headed : LogLineKind.Indented;
        for (var i = messages.Count - 1; i >= 0 && lines.Count < limit; i--) {
            var text = messages[i] ?? string.Empty;
            var stop = text.Length;
            var more = true;
            while (more) {
                var separator = stop > 0 ? text.LastIndexOf('\n', stop - 1) : -1;
                var start = separator + 1;
                var length = stop - start;
                bool endsWithReturn = length > 0 && text[stop - 1] == '\r';
                if (endsWithReturn) {
                    length--;
                }
                lines.Add(new LogLine(entry, text, start, length, kind));
                stop = separator;
                more = separator >= 0 && lines.Count < limit;
            }
        }
        bool needsHeader = messages.Count != 1 && lines.Count < limit;
        if (needsHeader) {
            lines.Add(new LogLine(entry, null, 0, 0, LogLineKind.Header));
        }
    }
    public static int Write(LogLine line, ref char[] buffer) {
        var nameLength = line.Entry.LogName?.Length ?? 0;
        var capacity = HeaderPadding + nameLength + TabWidth * line.Length;
        bool bufferIsSmall = buffer.Length < capacity;
        if (bufferIsSmall) {
            buffer = new char[Math.Max(capacity, 2 * buffer.Length)];
        }
        var position = 0;
        switch (line.Kind) {
            case LogLineKind.Header:
                position = WriteHeader(buffer, position, line.Entry);
                break;
            case LogLineKind.Headed:
                position = WriteHeader(buffer, position, line.Entry);
                buffer[position++] = ' ';
                position = WriteSegment(buffer, position, line.Message, line.Start, line.Length);
                break;
            default:
                buffer[position++] = ' ';
                buffer[position++] = ' ';
                position = WriteSegment(buffer, position, line.Message, line.Start, line.Length);
                break;
        }
        return position;
    }

    public static string Format(LogLine line) {
        var buffer = new char[HeaderPadding];
        var length = Write(line, ref buffer);
        return new string(buffer, 0, length);
    }
}
