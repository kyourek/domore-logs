using Domore.Logs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Domore.Windows;

/// <summary>
/// Shows a <see cref="Controls.LogsControl"/> receiving a simulated workload, with settings for its
/// threshold, source filter, line limit, font, and theme.
/// </summary>
public sealed partial class LogSampleWindow {
    private static readonly ILog Log = Logging.For(typeof(LogSampleWindow));

    private readonly LogGenerator Generator = new();
    private readonly ObservableCollection<string> ShownSources = [];
    private readonly CancellationTokenSource Running = new();
    private readonly DispatcherTimer StatusTimer;

    private void UpdateStatus(object sender, EventArgs e) {
        StatusText.Text = $"{Generator.Logged:N0} entries logged";
    }

    private void RateBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        Generator.Rate = (int)((ComboBox)sender).SelectedItem;
    }

    private void Source_Changed(object sender, RoutedEventArgs e) {
        var source = (CheckBox)sender;
        var name = (string)source.Content;
        var shown = source.IsChecked == true;
        if (shown) {
            ShownSources.Add(name);
        }
        else {
            ShownSources.Remove(name);
        }
    }

    private void DarkTheme_Changed(object sender, RoutedEventArgs e) {
        var dark = DarkTheme.IsChecked == true;
        LogView.Style = dark
            ? (Style)FindResource("DarkLogStyle")
            : null;
    }

    private async void Burst_Click(object sender, RoutedEventArgs e) {
        BurstButton.IsEnabled = false;
        try {
            await Generator.Burst(100_000);
        }
        finally {
            BurstButton.IsEnabled = true;
        }
    }

    private void Exception_Click(object sender, RoutedEventArgs e) {
        LogGenerator.LogException();
    }

    private void Unicode_Click(object sender, RoutedEventArgs e) {
        LogGenerator.LogUnicodeAndLongLine();
    }

    /// <inheritdoc/>
    protected override void OnContentRendered(EventArgs e) {
        base.OnContentRendered(e);
        Log.Info(
            "Welcome to the Domore.Logs.Windows sample.",
            "Entries stream in from worker threads; the view follows them while it is scrolled to the end.",
            "Click a row to select it, and Shift-click or drag to select a range. Selecting pauses following.",
            "Ctrl+C copies the selection, or every row without one. Escape clears it, and End follows again.");
        _ = Generator.Run(Running.Token);
    }

    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e) {
        StatusTimer.Stop();
        Running.Cancel();
        base.OnClosed(e);
    }

    /// <summary>
    /// Gets the selectable workload rates, in entries per second.
    /// </summary>
    public static IReadOnlyList<int> Rates { get; } = [0, 10, 100, 1_000, 10_000, 100_000];

    /// <summary>
    /// Gets the selectable display thresholds.
    /// </summary>
    public static IReadOnlyList<LogSeverity> Thresholds { get; } = [
        LogSeverity.Debug,
        LogSeverity.Info,
        LogSeverity.Warn,
        LogSeverity.Error,
        LogSeverity.Critical
    ];

    /// <summary>
    /// Gets the selectable line limits.
    /// </summary>
    public static IReadOnlyList<int> LineLimits { get; } = [1_000, 10_000, 100_000];

    /// <summary>
    /// Gets the names of the simulated log sources.
    /// </summary>
    public static IReadOnlyList<string> Sources { get; } = LogGenerator.SourceNames;

    /// <summary>
    /// Initializes the window and its log view.
    /// </summary>
    public LogSampleWindow() {
        InitializeComponent();
        LogView.LogNames = ShownSources;
        StatusTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, UpdateStatus, Dispatcher);
    }
}
