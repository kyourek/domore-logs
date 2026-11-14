# Domore.Logs.Windows

WPF presentation for Domore.Logs. The package provides `LogControl`, which
displays entries written through `ILog` as they are logged.

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:logs="clr-namespace:Domore.Windows.Controls;assembly=Domore.Logs.Windows">
  <logs:LogControl Threshold="Debug" LineLimit="5000" />
</Window>
```

The control subscribes through `Logging.Subscribe` while it is loaded and
unsubscribes when it is unloaded. No logging configuration is required. Entries
may be logged on any thread. A bounded queue hands entries to the dispatcher in
batches of at most 64, allowing input and rendering between batches. Pending
overflow discards the oldest entries; this viewer is a window onto recent logs,
not durable log storage.

Only visible lines have drawings. WPF's text formatter handles Unicode shaping,
bidirectional text, and font fallback. Drawings are retained while visible and
reused during scrolling. Very long lines remain a single horizontally scrollable
row, including formatter continuations. The view follows new entries while
scrolled to the end and preserves the visible history when scrolled away.
The horizontal scroll range is the widest line drawn so far.

Click a row to select it, Shift-click or drag to select a range, and press Ctrl+C
to copy selected rows. With no selection, Ctrl+C copies all retained rows.
Arrow keys, Page Up/Down, Home, and End navigate the view. End resumes following
new entries. Selecting rows pauses following; Escape clears the selection.
Selection tracks row identity as old history is discarded.

`Threshold` sets the lowest severity displayed. The default is `Info`, and
`None` displays nothing. Set `LogTypes`, `LogNames`, or both to display only
entries whose `LogType` or `LogName` is listed. When both are null or empty,
entries of every type are displayed. Collections that implement
`INotifyCollectionChanged` are observed for changes:

```csharp
LogView.LogTypes = [typeof(MainWindow)];
LogView.LogNames = ["Worker"];
```

`LineLimit` is the number of lines retained, 10,000 by default. When it is
reached, the oldest lines are discarded as new lines arrive. Entries that arrive
faster than the dispatcher can draw them are bounded by the same entry count.
Multiline entries are scanned from the end and only the newest lines that fit
are staged. These are count bounds, not byte bounds: message strings are kept
by reference, and an individual message can be arbitrarily large.

Style the control with `Background`, `Foreground`, `BorderBrush`,
`BorderThickness`, `Padding`, and the font properties, which default to
Consolas. `DebugForeground`, `InfoForeground`, `WarnForeground`,
`ErrorForeground`, and `CriticalForeground` color entries by severity; a null
brush uses `Foreground`. `ScrollViewerStyle` styles the scroll viewer.

```xml
<logs:LogControl Background="#1E1E1E"
                 Foreground="Gainsboro"
                 FontSize="12"
                 InfoForeground="LightSkyBlue"
                 ErrorForeground="OrangeRed" />
```

The project targets `net462`, `net48`, `net8.0-windows`, and `net10.0-windows`,
and requires WPF.
