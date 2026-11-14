using Domore.Logs;
using System;
using System.Collections.Generic;

namespace Domore.Windows.Controls;

internal sealed class LogFilter {
    private readonly HashSet<Type> Types = [];
    private readonly HashSet<string> Names = new(StringComparer.Ordinal);

    private bool Matches(Type type, string name) {
        bool unrestricted = Types.Count == 0 && Names.Count == 0;
        if (unrestricted) {
            return true;
        }
        bool typeMatches = type is not null && Types.Contains(type);
        if (typeMatches) {
            return true;
        }
        return name is not null && Names.Contains(name);
    }

    public LogSeverity Threshold { get; }

    public LogFilter(LogSeverity threshold, IEnumerable<Type> types, IEnumerable<string> names) {
        Threshold = threshold;
        bool hasTypes = types is not null;
        if (hasTypes) {
            foreach (var type in types) {
                bool typeIsNull = type is null;
                if (typeIsNull) {
                    continue;
                }
                Types.Add(type);
            }
        }
        bool hasNames = names is not null;
        if (hasNames) {
            foreach (var name in names) {
                bool nameIsNull = name is null;
                if (nameIsNull) {
                    continue;
                }
                Names.Add(name);
            }
        }
    }

    public LogSeverity ThresholdFor(Type type) {
        bool matches = type is not null && Matches(type, type.Name);
        return matches
            ? Threshold
            : LogSeverity.None;
    }

    public bool Includes(LogSeverity severity, Type type, string name) {
        bool severityIsShown =
            Threshold != LogSeverity.None &&
            severity != LogSeverity.None &&
            Threshold <= severity;
        return severityIsShown && Matches(type, name);
    }
}
