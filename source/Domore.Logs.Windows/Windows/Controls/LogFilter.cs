using Domore.Logs;
using System;
using System.Collections.Generic;

namespace Domore.Windows.Controls;

internal sealed class LogFilter {
    private readonly HashSet<Type> Types = [];
    private readonly HashSet<string> Names = new(StringComparer.Ordinal);

    private bool Matches(Type type, string name) {
        var unrestricted = Types.Count == 0 && Names.Count == 0;
        if (unrestricted) {
            return true;
        }
        var typeMatches = type is not null && Types.Contains(type);
        if (typeMatches) {
            return true;
        }
        return name is not null && Names.Contains(name);
    }

    public LogSeverity Threshold { get; }

    public LogFilter(LogSeverity threshold, IEnumerable<Type> types, IEnumerable<string> names) {
        Threshold = threshold;
        var hasTypes = types is not null;
        if (hasTypes) {
            foreach (var type in types) {
                if (type is null) {
                    continue;
                }
                Types.Add(type);
            }
        }
        var hasNames = names is not null;
        if (hasNames) {
            foreach (var name in names) {
                if (name is null) {
                    continue;
                }
                Names.Add(name);
            }
        }
    }

    public LogSeverity ThresholdFor(Type type) {
        var matches = type is not null && Matches(type, type.Name);
        return matches ? Threshold : LogSeverity.None;
    }

    public bool Includes(LogSeverity severity, Type type, string name) {
        var severityIsShown = Threshold != LogSeverity.None &&
                              severity != LogSeverity.None &&
                              Threshold <= severity;
        return severityIsShown && Matches(type, name);
    }
}
