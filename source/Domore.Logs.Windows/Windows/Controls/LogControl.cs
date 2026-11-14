using Domore.Logs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Domore.Windows.Controls;

/// <summary>
/// Displays entries written through <see cref="ILog"/> as they are logged.
/// </summary>
/// <remarks>
/// <para>
/// The control subscribes to logging through <see cref="Logging.Subscribe(ILogSubscription)"/>
/// while it is loaded and unsubscribes when it is unloaded. Entries may be logged on any thread;
/// they are queued and drawn in batches on the control's dispatcher. Only lines inside the
/// viewport are drawn, and the oldest lines are discarded once <see cref="LogLineLimit"/> is reached.
/// The view follows new entries while it is scrolled to the end.
/// </para>
/// <para>
/// Click a row to select it, and Shift-click or drag to select a range. Selecting pauses following
/// new entries; End resumes it, and Escape clears the selection. <see cref="ApplicationCommands.Copy"/>
/// (Ctrl+C) copies the selected rows, or every retained row when nothing is selected. The arrow keys,
/// Page Up, Page Down, Home, and End scroll the view.
/// </para>
/// </remarks>
public sealed class LogControl : Control {
    static LogControl() {
        DefaultStyleKeyProperty.OverrideMetadata(
            forType: typeof(LogControl),
            typeMetadata: new FrameworkPropertyMetadata(typeof(LogControl)));
    }

    private const string ScrollViewerTemplatePartName = "PART_LogScrollViewer";
    private const int LogLineLimitDefault = 10000;

    private ScrollViewer ScrollViewerPart;

    private static DependencyProperty RegisterForeground(string name) {
        return DependencyProperty.Register(
            name: name,
            propertyType: typeof(Brush),
            ownerType: typeof(LogControl),
            typeMetadata: new PropertyMetadata(
                defaultValue: null,
                propertyChangedCallback: (s, e) => ((LogControl)s).Presenter.InvalidateLines()));
    }

    private void LogControl_Loaded(object sender, RoutedEventArgs e) {
        Attach();
    }

    private void LogControl_Unloaded(object sender, RoutedEventArgs e) {
        Detach();
    }

    private void FilterCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
        UpdateFilter();
    }

    private void FilterCollectionChanged(object oldValue, object newValue) {
        var oldCollection = oldValue as INotifyCollectionChanged;
        var oldValueIsObservable = oldCollection is not null;
        if (oldValueIsObservable) {
            CollectionChangedEventManager.RemoveHandler(oldCollection, FilterCollection_CollectionChanged);
        }
        var newCollection = newValue as INotifyCollectionChanged;
        var newValueIsObservable = newCollection is not null;
        if (newValueIsObservable) {
            CollectionChangedEventManager.AddHandler(newCollection, FilterCollection_CollectionChanged);
        }
        UpdateFilter();
    }

    private void UpdateFilter() {
        Subscription.Filter = new LogFilter(LogThreshold, LogTypes, LogNames);
    }

    private void Copy(object sender, ExecutedRoutedEventArgs e) {
        var text = Presenter.CopyText();
        var hasText = text.Length > 0;
        if (hasText) {
            try {
                Clipboard.SetText(text);
            }
            catch (ExternalException) {
                /*
                 * Another application holds the clipboard. Like TextBox, the
                 * copy is abandoned rather than failing the application.
                 */
            }
        }
        e.Handled = true;
    }

    private void Deliver(IReadOnlyCollection<ILogEntry> entries) {
        var filter = Subscription.Filter;
        var added = false;
        foreach (var entry in entries) {
            /*
             * The filter may have changed while the entry was pending.
             */
            var included = filter.Includes(entry.LogSeverity, entry.LogType, entry.LogName);
            if (included) {
                LogLineFormatter.Split(entry, Presenter.Lines);
                added = true;
            }
        }
        if (added) {
            Presenter.LinesAdded();
        }
    }

    internal LogPresenter Presenter { get; }
    internal LogSubscription Subscription { get; }

    /*
     * Returns true when the key was used. Escape is used only to clear a
     * selection, so it otherwise still reaches the window, such as a dialog's
     * Cancel button.
     */
    internal bool HandleKey(Key key) {
        switch (key) {
            case Key.Escape:
                return Presenter.ClearSelection();
            case Key.Up:
                Presenter.LineUp();
                return true;
            case Key.Down:
                Presenter.LineDown();
                return true;
            case Key.Left:
                Presenter.LineLeft();
                return true;
            case Key.Right:
                Presenter.LineRight();
                return true;
            case Key.PageUp:
                Presenter.PageUp();
                return true;
            case Key.PageDown:
                Presenter.PageDown();
                return true;
            case Key.Home:
                Presenter.SetVerticalOffset(0);
                return true;
            case Key.End:
                Presenter.SetVerticalOffset(double.PositiveInfinity);
                return true;
            default:
                return false;
        }
    }

    internal bool Attach() {
        return Logging.Subscribe(Subscription);
    }

    internal bool Detach() {
        return Logging.Unsubscribe(Subscription);
    }

    internal Brush GetSeverityForeground(LogSeverity severity) {
        var brush = severity switch {
            LogSeverity.Debug => LogDebugForeground,
            LogSeverity.Info => LogInfoForeground,
            LogSeverity.Warn => LogWarnForeground,
            LogSeverity.Error => LogErrorForeground,
            LogSeverity.Critical => LogCriticalForeground,
            _ => null
        };
        return brush ?? Foreground;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);
        var unhandled = e.Handled == false;
        if (unhandled) {
            e.Handled = HandleKey(e.Key);
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
        base.OnPropertyChanged(e);
        var property = e.Property;
        var fontChanged =
            property == FontFamilyProperty ||
            property == FontSizeProperty ||
            property == FontStretchProperty ||
            property == FontStyleProperty ||
            property == FontWeightProperty;
        if (fontChanged) {
            Presenter?.InvalidateFont();
        }
        var foregroundChanged = property == ForegroundProperty;
        if (foregroundChanged) {
            Presenter?.InvalidateLines();
        }
    }

    /// <summary>
    /// Identifies the <see cref="LogThreshold"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LogThresholdProperty = DependencyProperty.Register(
        name: nameof(LogThreshold),
        propertyType: typeof(LogSeverity),
        ownerType: typeof(LogControl),
        typeMetadata: new PropertyMetadata(
            defaultValue: LogSeverity.Info,
            propertyChangedCallback: (s, e) => ((LogControl)s).UpdateFilter()));

    /// <summary>
    /// Gets or sets the lowest severity of entries that are displayed.
    /// </summary>
    /// <value>
    /// The minimum severity to display. The default is <see cref="LogSeverity.Info"/>.
    /// <see cref="LogSeverity.None"/> displays no entries.
    /// </value>
    /// <remarks>
    /// Entries already displayed are kept when the threshold changes.
    /// </remarks>
    public LogSeverity LogThreshold {
        get => (LogSeverity)GetValue(LogThresholdProperty);
        set => SetValue(LogThresholdProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="LogTypes"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LogTypesProperty = DependencyProperty.Register(
        name: nameof(LogTypes),
        propertyType: typeof(IEnumerable<Type>),
        ownerType: typeof(LogControl),
        typeMetadata: new PropertyMetadata(
            defaultValue: null,
            propertyChangedCallback: (s, e) => ((LogControl)s).FilterCollectionChanged(e.OldValue, e.NewValue)));

    /// <summary>
    /// Gets or sets the log types whose entries are displayed.
    /// </summary>
    /// <value>
    /// Types compared with <see cref="ILogEntry.LogType"/>, or null.
    /// </value>
    /// <remarks>
    /// An entry is displayed when its type is in <see cref="LogTypes"/> or its name is in
    /// <see cref="LogNames"/>. When both are null or empty, entries of every type are displayed.
    /// Changes to a collection that implements <see cref="INotifyCollectionChanged"/> are applied
    /// when the collection raises its change notification.
    /// </remarks>
    public IEnumerable<Type> LogTypes {
        get => GetValue(LogTypesProperty) as IEnumerable<Type>;
        set => SetValue(LogTypesProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="LogNames"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LogNamesProperty = DependencyProperty.Register(
        name: nameof(LogNames),
        propertyType: typeof(IEnumerable<string>),
        ownerType: typeof(LogControl),
        typeMetadata: new PropertyMetadata(
            defaultValue: null,
            propertyChangedCallback: (s, e) => ((LogControl)s).FilterCollectionChanged(e.OldValue, e.NewValue)));

    /// <summary>
    /// Gets or sets the log names whose entries are displayed.
    /// </summary>
    /// <value>
    /// Names compared ordinally with <see cref="ILogEntry.LogName"/>, or null.
    /// </value>
    /// <remarks>
    /// An entry is displayed when its type is in <see cref="LogTypes"/> or its name is in
    /// <see cref="LogNames"/>. When both are null or empty, entries of every type are displayed.
    /// Changes to a collection that implements <see cref="INotifyCollectionChanged"/> are applied
    /// when the collection raises its change notification.
    /// </remarks>
    public IEnumerable<string> LogNames {
        get => GetValue(LogNamesProperty) as IEnumerable<string>;
        set => SetValue(LogNamesProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="LogLineLimit"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LogLineLimitProperty = DependencyProperty.Register(
        name: nameof(LogLineLimit),
        propertyType: typeof(int),
        ownerType: typeof(LogControl),
        typeMetadata: new PropertyMetadata(
            defaultValue: LogLineLimitDefault,
            propertyChangedCallback: (s, e) => {
                var self = (LogControl)s;
                var limit = (int)e.NewValue;
                self.Subscription.PendingLimit = limit;
                self.Presenter.SetLineLimit(limit);
            }),
        validateValueCallback: value => value is int limit && limit > 0);

    /// <summary>
    /// Gets or sets the maximum number of lines retained by the control.
    /// </summary>
    /// <value>
    /// A positive number of lines. The default is 10,000.
    /// </value>
    /// <remarks>
    /// When the limit is reached, the oldest lines are discarded as new lines are added.
    /// Lowering the limit discards the oldest lines immediately.
    /// </remarks>
    public int LogLineLimit {
        get => (int)GetValue(LogLineLimitProperty);
        set => SetValue(LogLineLimitProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="LogDebugForeground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LogDebugForegroundProperty = RegisterForeground(nameof(LogDebugForeground));

    /// <summary>
    /// Gets or sets the brush used to draw <see cref="LogSeverity.Debug"/> entries.
    /// </summary>
    /// <value>
    /// A brush, or null to use <see cref="Control.Foreground"/>.
    /// </value>
    public Brush LogDebugForeground {
        get => GetValue(LogDebugForegroundProperty) as Brush;
        set => SetValue(LogDebugForegroundProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="LogInfoForeground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LogInfoForegroundProperty = RegisterForeground(nameof(LogInfoForeground));

    /// <summary>
    /// Gets or sets the brush used to draw <see cref="LogSeverity.Info"/> entries.
    /// </summary>
    /// <value>
    /// A brush, or null to use <see cref="Control.Foreground"/>.
    /// </value>
    public Brush LogInfoForeground {
        get => GetValue(LogInfoForegroundProperty) as Brush;
        set => SetValue(LogInfoForegroundProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="LogWarnForeground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LogWarnForegroundProperty = RegisterForeground(nameof(LogWarnForeground));

    /// <summary>
    /// Gets or sets the brush used to draw <see cref="LogSeverity.Warn"/> entries.
    /// </summary>
    /// <value>
    /// A brush, or null to use <see cref="Control.Foreground"/>.
    /// </value>
    public Brush LogWarnForeground {
        get => GetValue(LogWarnForegroundProperty) as Brush;
        set => SetValue(LogWarnForegroundProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="LogErrorForeground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LogErrorForegroundProperty = RegisterForeground(nameof(LogErrorForeground));

    /// <summary>
    /// Gets or sets the brush used to draw <see cref="LogSeverity.Error"/> entries.
    /// </summary>
    /// <value>
    /// A brush, or null to use <see cref="Control.Foreground"/>.
    /// </value>
    public Brush LogErrorForeground {
        get => GetValue(LogErrorForegroundProperty) as Brush;
        set => SetValue(LogErrorForegroundProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="LogCriticalForeground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LogCriticalForegroundProperty =
        RegisterForeground(nameof(LogCriticalForeground));

    /// <summary>
    /// Gets or sets the brush used to draw <see cref="LogSeverity.Critical"/> entries.
    /// </summary>
    /// <value>
    /// A brush, or null to use <see cref="Control.Foreground"/>.
    /// </value>
    public Brush LogCriticalForeground {
        get => GetValue(LogCriticalForegroundProperty) as Brush;
        set => SetValue(LogCriticalForegroundProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="LogSelectionBackground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LogSelectionBackgroundProperty = DependencyProperty.Register(
        name: nameof(LogSelectionBackground),
        propertyType: typeof(Brush),
        ownerType: typeof(LogControl),
        typeMetadata: new PropertyMetadata(
            defaultValue: null,
            propertyChangedCallback: (s, e) => ((LogControl)s).Presenter.InvalidateArrange()));

    /// <summary>
    /// Gets or sets the brush drawn behind selected rows.
    /// </summary>
    /// <value>
    /// A brush, drawn as given so that its own opacity applies, or null to use the system
    /// highlight color at 30% opacity.
    /// </value>
    public Brush LogSelectionBackground {
        get => GetValue(LogSelectionBackgroundProperty) as Brush;
        set => SetValue(LogSelectionBackgroundProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="LogScrollViewerStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LogScrollViewerStyleProperty = DependencyProperty.Register(
        name: nameof(LogScrollViewerStyle),
        propertyType: typeof(Style),
        ownerType: typeof(LogControl),
        typeMetadata: new PropertyMetadata(
            defaultValue: null));

    /// <summary>
    /// Gets or sets the style applied to the control's scroll viewer.
    /// </summary>
    /// <value>
    /// A style targeting <see cref="ScrollViewer"/>, or null to use the default style, which shows
    /// each scroll bar when it is needed.
    /// </value>
    public Style LogScrollViewerStyle {
        get => GetValue(LogScrollViewerStyleProperty) as Style;
        set => SetValue(LogScrollViewerStyleProperty, value);
    }

    /// <summary>
    /// Initializes a new log display control.
    /// </summary>
    public LogControl() {
        Presenter = new LogPresenter(this, LogLineLimitDefault);
        Presenter.SetBinding(MarginProperty, new Binding(nameof(Padding)) {
            Source = this
        });
        Subscription = new LogSubscription(
            dispatcher: Dispatcher,
            deliver: Deliver,
            filter: new LogFilter(LogSeverity.Info, null, null),
            pendingLimit: LogLineLimitDefault);
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, Copy));
        Loaded += LogControl_Loaded;
        Unloaded += LogControl_Unloaded;
    }

    /// <inheritdoc/>
    public override void OnApplyTemplate() {
        var hadScrollViewer = ScrollViewerPart is not null;
        if (hadScrollViewer) {
            ScrollViewerPart.Content = null;
        }
        base.OnApplyTemplate();
        ScrollViewerPart = GetTemplateChild(ScrollViewerTemplatePartName) as ScrollViewer;
        var hasScrollViewer = ScrollViewerPart is not null;
        if (hasScrollViewer) {
            ScrollViewerPart.CanContentScroll = true;
            ScrollViewerPart.Content = Presenter;
        }
    }
}
