# Domore.Logs.Windows sample

A small WPF application that shows `LogControl` displaying a live, simulated
workload. Run it on Windows with .NET 10:

```powershell
dotnet run --project samples/Domore.Logs.Windows.Sample
```

The workload writes through ordinary `ILog` instances on worker threads from
three sources, `Network`, `Database`, and `Scheduler`, at every severity. The
control subscribes to Domore.Logs itself, so the application needs no logging
configuration and never hands entries to the control.

Things to try:

- **Entries per second** changes the workload from paused to 100,000 entries
  per second. The view follows new entries while it is scrolled to the end.
- **Burst 100,000 entries** logs from four threads at once while the window
  stays responsive.
- **Threshold** binds to `LogThreshold`. Entries below it are never formatted,
  because the workload checks `ILog.Enabled` first.
- **Show only these sources** edits an `ObservableCollection<string>` assigned
  to `LogNames`; the control applies each change as it happens. `LogTypes`
  filters by `Type` in the same way.
- **Line limit** binds to `LogLineLimit`. The oldest lines are discarded once the
  limit is reached.
- **Font size** and **Dark theme** restyle the control through its font and
  brush properties. See `DarkLogStyle` in
  [LogSampleWindow.xaml](LogSampleWindow.xaml).
- **Log an exception** writes a multi-line entry, and **Log Unicode and a long
  line** shows font fallback, right-to-left text, and horizontal scrolling.
- Click a row to select it, and Shift-click or drag to select a range.
  Selecting pauses following; End resumes it. Ctrl+C copies the selection, or
  every retained row without one, and Escape clears it.
