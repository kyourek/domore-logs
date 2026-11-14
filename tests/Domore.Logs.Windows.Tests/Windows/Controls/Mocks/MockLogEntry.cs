using Domore.Logs;
using System;
using System.Collections.Generic;

namespace Domore.Windows.Controls.Mocks;

internal sealed class MockLogEntry : ILogEntry {
    public Type LogType { get; set; } = typeof(MockLogEntry);
    public string LogName { get; set; } = "Mock";
    public DateTime LogDate { get; set; } = new DateTime(2026, 11, 13, 1, 2, 3, 45, DateTimeKind.Local);
    public LogSeverity LogSeverity { get; set; } = LogSeverity.Info;
    public IEnumerable<string> LogList { get; set; } = [];
}
