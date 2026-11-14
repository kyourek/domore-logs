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
    private const char LineSeparator = (char)0x2028;
    private const char ParagraphSeparator = (char)0x2029;

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

    private static void AddMessage(LogLineBuffer lines, LogLine header, string message, LogLineKind kind) {
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
            lines.Add(new LogLine(header.Date, header.Severity, header.Name, text, start, length, kind));
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

    private static int WriteHeader(char[] buffer, int position, LogLine line) {
        var time = line.Date.ToLocalTime();
        position = WriteDigits(buffer, position, time.Hour, 2);
        buffer[position++] = ':';
        position = WriteDigits(buffer, position, time.Minute, 2);
        buffer[position++] = ':';
        position = WriteDigits(buffer, position, time.Second, 2);
        buffer[position++] = '.';
        position = WriteDigits(buffer, position, time.Millisecond, 3);
        buffer[position++] = ' ';
        position = WriteText(buffer, position, Abbreviate(line.Severity));
        buffer[position++] = ' ';
        return WriteText(buffer, position, line.Name);
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
            /*
             * WPF text layout breaks lines at controls and at the line and
             * paragraph separators, which would end a single-row display line.
             */
            bool breaksLine = char.IsControl(c) || c == LineSeparator || c == ParagraphSeparator;
            buffer[position++] = breaksLine ? ' ' : c;
        }
        return position;
    }

    /*
     * Lines are added directly to the bounded buffer, so an entry with more
     * lines than the buffer holds needs no further storage.
     */
    public static void Split(ILogEntry entry, LogLineBuffer lines) {
        var messages = entry.LogList as IList<string> ?? entry.LogList?.ToArray() ?? [];
        var header = new LogLine(entry.LogDate, entry.LogSeverity, entry.LogName, null, 0, 0, LogLineKind.Header);
        bool single = messages.Count == 1;
        if (single) {
            AddMessage(lines, header, messages[0], LogLineKind.Headed);
            return;
        }
        lines.Add(header);
        for (var i = 0; i < messages.Count; i++) {
            AddMessage(lines, header, messages[i], LogLineKind.Indented);
        }
    }

    public static int Write(LogLine line, ref char[] buffer) {
        var capacity = HeaderPadding + line.Name.Length + TabWidth * line.Length;
        bool bufferIsSmall = buffer.Length < capacity;
        if (bufferIsSmall) {
            buffer = new char[Math.Max(capacity, 2 * buffer.Length)];
        }
        var position = 0;
        switch (line.Kind) {
            case LogLineKind.Header:
                position = WriteHeader(buffer, position, line);
                break;
            case LogLineKind.Headed:
                position = WriteHeader(buffer, position, line);
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
