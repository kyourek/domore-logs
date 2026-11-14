using Domore.Logs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Domore.Windows.Controls;

/// <summary>
/// Displays entries written through <see cref="ILog"/> as they are logged.
/// </summary>
/// <remarks>
/// The control subscribes to logging through <see cref="Logging.Subscribe(ILogSubscription)"/>
/// while it is loaded and unsubscribes when it is unloaded. Entries may be logged on any thread;
/// they are queued and drawn in batches on the control's dispatcher. Only lines inside the
/// viewport are drawn, and the oldest lines are discarded once <see cref="LineLimit"/> is reached.
/// The view follows new entries while it is scrolled to the end.
/// </remarks>
public sealed class LogControl : Control {
    static LogControl() {
        DefaultStyleKeyProperty.OverrideMetadata(
            forType: typeof(LogControl),
            typeMetadata: new FrameworkPropertyMetadata(typeof(LogControl)));
    }

    private const string ScrollViewerTemplatePartName = "PART_ScrollViewer";
    private const int DefaultLineLimit = 10000;

    private readonly List<LogLine> DeliveredLines = [];
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
        bool oldValueIsObservable = oldCollection is not null;
        if (oldValueIsObservable) {
            CollectionChangedEventManager.RemoveHandler(oldCollection, FilterCollection_CollectionChanged);
        }
        var newCollection = newValue as INotifyCollectionChanged;
        bool newValueIsObservable = newCollection is not null;
        if (newValueIsObservable) {
            CollectionChangedEventManager.AddHandler(newCollection, FilterCollection_CollectionChanged);
        }
        UpdateFilter();
    }

    private void UpdateFilter() {
        Subscription.Filter = new LogFilter(Threshold, LogTypes, LogNames);
    }

    private void Deliver(IReadOnlyCollection<ILogEntry> entries) {
        var filter = Subscription.Filter;
        foreach (var entry in entries) {
            /*
             * The filter may have changed while the entry was pending.
             */
            bool included = filter.Includes(entry.LogSeverity, entry.LogType, entry.LogName);
            if (included) {
                LogLineFormatter.Split(entry, DeliveredLines);
            }
        }
        bool hasLines = DeliveredLines.Count > 0;
        if (hasLines) {
            Presenter.Append(DeliveredLines);
            DeliveredLines.Clear();
        }
    }

    internal LogPresenter Presenter { get; }
    internal LogSubscription Subscription { get; }

    internal bool Attach() {
        return Logging.Subscribe(Subscription);
    }

    internal bool Detach() {
        return Logging.Unsubscribe(Subscription);
    }

    internal Brush GetSeverityForeground(LogSeverity severity) {
        var brush = severity switch {
            LogSeverity.Debug => DebugForeground,
            LogSeverity.Info => InfoForeground,
            LogSeverity.Warn => WarnForeground,
            LogSeverity.Error => ErrorForeground,
            LogSeverity.Critical => CriticalForeground,
            _ => null
        };
        return brush ?? Foreground;
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
        base.OnPropertyChanged(e);
        var property = e.Property;
        bool fontChanged =
            property == FontFamilyProperty ||
            property == FontSizeProperty ||
            property == FontStretchProperty ||
            property == FontStyleProperty ||
            property == FontWeightProperty;
        if (fontChanged) {
            Presenter?.InvalidateFont();
        }
        bool foregroundChanged = property == ForegroundProperty;
        if (foregroundChanged) {
            Presenter?.InvalidateLines();
        }
    }

    /// <summary>
    /// Identifies the <see cref="Threshold"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register(
        name: nameof(Threshold),
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
    public LogSeverity Threshold {
        get => (LogSeverity)GetValue(ThresholdProperty);
        set => SetValue(ThresholdProperty, value);
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
    /// Identifies the <see cref="LineLimit"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LineLimitProperty = DependencyProperty.Register(
        name: nameof(LineLimit),
        propertyType: typeof(int),
        ownerType: typeof(LogControl),
        typeMetadata: new PropertyMetadata(
            defaultValue: DefaultLineLimit,
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
    public int LineLimit {
        get => (int)GetValue(LineLimitProperty);
        set => SetValue(LineLimitProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="DebugForeground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty DebugForegroundProperty = RegisterForeground(nameof(DebugForeground));

    /// <summary>
    /// Gets or sets the brush used to draw <see cref="LogSeverity.Debug"/> entries.
    /// </summary>
    /// <value>
    /// A brush, or null to use <see cref="Control.Foreground"/>.
    /// </value>
    public Brush DebugForeground {
        get => GetValue(DebugForegroundProperty) as Brush;
        set => SetValue(DebugForegroundProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="InfoForeground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty InfoForegroundProperty = RegisterForeground(nameof(InfoForeground));

    /// <summary>
    /// Gets or sets the brush used to draw <see cref="LogSeverity.Info"/> entries.
    /// </summary>
    /// <value>
    /// A brush, or null to use <see cref="Control.Foreground"/>.
    /// </value>
    public Brush InfoForeground {
        get => GetValue(InfoForegroundProperty) as Brush;
        set => SetValue(InfoForegroundProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="WarnForeground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty WarnForegroundProperty = RegisterForeground(nameof(WarnForeground));

    /// <summary>
    /// Gets or sets the brush used to draw <see cref="LogSeverity.Warn"/> entries.
    /// </summary>
    /// <value>
    /// A brush, or null to use <see cref="Control.Foreground"/>.
    /// </value>
    public Brush WarnForeground {
        get => GetValue(WarnForegroundProperty) as Brush;
        set => SetValue(WarnForegroundProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ErrorForeground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ErrorForegroundProperty = RegisterForeground(nameof(ErrorForeground));

    /// <summary>
    /// Gets or sets the brush used to draw <see cref="LogSeverity.Error"/> entries.
    /// </summary>
    /// <value>
    /// A brush, or null to use <see cref="Control.Foreground"/>.
    /// </value>
    public Brush ErrorForeground {
        get => GetValue(ErrorForegroundProperty) as Brush;
        set => SetValue(ErrorForegroundProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="CriticalForeground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CriticalForegroundProperty =
        RegisterForeground(nameof(CriticalForeground));

    /// <summary>
    /// Gets or sets the brush used to draw <see cref="LogSeverity.Critical"/> entries.
    /// </summary>
    /// <value>
    /// A brush, or null to use <see cref="Control.Foreground"/>.
    /// </value>
    public Brush CriticalForeground {
        get => GetValue(CriticalForegroundProperty) as Brush;
        set => SetValue(CriticalForegroundProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ScrollViewerStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ScrollViewerStyleProperty = DependencyProperty.Register(
        name: nameof(ScrollViewerStyle),
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
    public Style ScrollViewerStyle {
        get => GetValue(ScrollViewerStyleProperty) as Style;
        set => SetValue(ScrollViewerStyleProperty, value);
    }

    /// <summary>
    /// Initializes a new log display control.
    /// </summary>
    public LogControl() {
        Presenter = new LogPresenter(this, DefaultLineLimit);
        Presenter.SetBinding(MarginProperty, new Binding(nameof(Padding)) {
            Source = this
        });
        Subscription = new LogSubscription(
            dispatcher: Dispatcher,
            deliver: Deliver,
            filter: new LogFilter(LogSeverity.Info, null, null),
            pendingLimit: DefaultLineLimit);
        Loaded += LogControl_Loaded;
        Unloaded += LogControl_Unloaded;
    }

    /// <inheritdoc/>
    public override void OnApplyTemplate() {
        bool hadScrollViewer = ScrollViewerPart is not null;
        if (hadScrollViewer) {
            ScrollViewerPart.Content = null;
        }
        base.OnApplyTemplate();
        ScrollViewerPart = GetTemplateChild(ScrollViewerTemplatePartName) as ScrollViewer;
        bool hasScrollViewer = ScrollViewerPart is not null;
        if (hasScrollViewer) {
            ScrollViewerPart.CanContentScroll = true;
            ScrollViewerPart.Content = Presenter;
        }
    }
}
