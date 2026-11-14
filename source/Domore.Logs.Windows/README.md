# Domore.Logs.Windows

WPF presentation for Domore.Logs. The package provides `LogsControl`, which
displays entries written through `ILog` as they are logged.

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:logs="clr-namespace:Domore.Windows.Controls;assembly=Domore.Logs.Windows">
  <logs:LogsControl LogThreshold="Debug" LogLineLimit="5000" />
</Window>
```

The control subscribes through `Logging.Subscribe` while it is loaded and
unsubscribes when it is unloaded. No logging configuration is required. Entries
may be logged on any thread; they are queued and drawn in batches on the
control's dispatcher. Lines are formatted and drawn only when they scroll into
view. Each visible line is drawn once as a glyph run and kept while it stays in
view, so scrolling and following new entries move existing lines rather than
redrawing them. A line with characters the font cannot map is drawn with
`FormattedText` so that font fallback applies. The view follows new entries
while it is scrolled to the end, and keeps its position otherwise. The
horizontal scroll range is the widest line drawn so far.

Click a row to select it, and Shift-click or drag to select a range. Press
Ctrl+C to copy the selected rows, or every retained row when nothing is
selected. Selecting pauses following new entries so the selection stays in
view; End resumes following, and Escape clears the selection. The selection
keeps its rows while older lines are discarded. The arrow keys, Page Up,
Page Down, Home, and End scroll the view.

`LogThreshold` sets the lowest severity displayed. The default is `Info`, and
`None` displays nothing. Set `LogTypes`, `LogNames`, or both to display only
entries whose `LogType` or `LogName` is listed. When both are null or empty,
entries of every type are displayed. Collections that implement
`INotifyCollectionChanged` are observed for changes:

```csharp
LogView.LogTypes = [typeof(MainWindow)];
LogView.LogNames = ["Worker"];
```

`LogLineLimit` is the number of lines retained, 10,000 by default. When it is
reached, the oldest lines are discarded as new lines arrive. Entries that arrive
faster than the dispatcher can draw them are bounded by the same limit.

Style the control with `Background`, `Foreground`, `BorderBrush`,
`BorderThickness`, `Padding`, and the font properties, which default to
Consolas. `LogDebugForeground`, `LogInfoForeground`, `LogWarnForeground`,
`LogErrorForeground`, and `LogCriticalForeground` color entries by severity;
a null brush uses `Foreground`. `LogSelectionBackground` sets the brush
drawn behind selected rows, as given so its own opacity applies; when it is
null, the system highlight color is drawn at 30% opacity.
`LogScrollViewerStyle` styles the scroll viewer.

```xml
<logs:LogsControl Background="#1E1E1E"
                 Foreground="Gainsboro"
                 FontSize="12"
                 LogInfoForeground="LightSkyBlue"
                 LogErrorForeground="OrangeRed" />
```

The project targets `net462`, `net48`, `net8.0-windows`, and `net10.0-windows`,
and requires WPF.
