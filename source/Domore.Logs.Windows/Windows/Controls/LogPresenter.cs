using Domore.Logs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Domore.Windows.Controls;

/*
 * Retains only visible line drawings. WPF formats Unicode text, including
 * shaping and fallback; formatter continuations share the same display row.
 */
internal sealed class LogPresenter : FrameworkElement, IScrollInfo {
    private const long RebaseDistance = 10000;

    private readonly LogControl Owner;
    private readonly ContainerVisual Content = new();
    private readonly Dictionary<long, DrawingVisual> Realized = [];
    private readonly List<long> Released = [];
    private readonly Stack<DrawingVisual> Reusable = new();

    private bool FontIsCurrent;
    private bool FollowsEnd = true;
    private bool MeasuredUnbounded;
    private long BaseSerial;
    private long? SelectionAnchor;
    private long SelectionEnd;
    private Typeface Typeface;
    private double FontEmSize;
    private double PixelsPerDip = 1;
    private double LineHeight = 1;
    private double Baseline;

    private double MaxHorizontalOffset => Math.Max(0, ExtentWidth - ViewportWidth);
    private double MaxVerticalOffset => Math.Max(0, ExtentHeight - ViewportHeight);

    private static double Clamp(double value, double max) {
        bool valueIsNaN = double.IsNaN(value);
        if (valueIsNaN) {
            return 0;
        }
        return Math.Max(0, Math.Min(value, max));
    }

    private double Snap(double value) {
        return Math.Round(value * PixelsPerDip) / PixelsPerDip;
    }

    private void EnsureFont() {
        if (FontIsCurrent) {
            return;
        }
        FontIsCurrent = true;
        var firstLine = VerticalOffset / LineHeight;
        ReleaseAll();
        PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        Typeface = new Typeface(Owner.FontFamily, Owner.FontStyle, Owner.FontWeight, Owner.FontStretch);
        FontEmSize = Owner.FontSize;
        LineHeight = Math.Max(1, Math.Ceiling(Typeface.FontFamily.LineSpacing * FontEmSize * PixelsPerDip) / PixelsPerDip);
        Baseline = Snap(Typeface.FontFamily.Baseline * FontEmSize);
        ExtentWidth = 0;
        ExtentHeight = Lines.Count * LineHeight;
        VerticalOffset = firstLine * LineHeight;
        UpdateOffsets();
    }

    private void UpdateOffsets() {
        VerticalOffset = FollowsEnd
            ? MaxVerticalOffset
            : Clamp(VerticalOffset, MaxVerticalOffset);
        HorizontalOffset = Clamp(HorizontalOffset, MaxHorizontalOffset);
        ScrollOwner?.InvalidateScrollInfo();
    }

    private void ReleaseAll() {
        Content.Children.Clear();
        Realized.Clear();
        Reusable.Clear();
    }

    private void LinesChanged(int removed) {
        ExtentHeight = Lines.Count * LineHeight;
        bool keepsPosition = FollowsEnd == false;
        if (keepsPosition) {
            /*
             * Keep the same lines in view while older lines are discarded.
             */
            VerticalOffset -= removed * LineHeight;
        }
        UpdateOffsets();
        if (MeasuredUnbounded) {
            InvalidateMeasure();
        }
        InvalidateArrange();
        InvalidateVisual();
    }

    private Brush GetBrush(LogSeverity severity) {
        return Owner.GetSeverityForeground(severity) ?? Brushes.Transparent;
    }

    private double Draw(TextFormatter formatter, DrawingVisual visual, LogLine line) {
        var text = LogLineFormatter.Format(line);
        var brush = GetBrush(line.Severity);
        var properties = new LogTextProperties(Typeface, FontEmSize, brush, PixelsPerDip);
        var source = new LogTextSource(text, properties, PixelsPerDip);
        var paragraph = new LogParagraphProperties(properties);
        var position = 0;
        List<IndexedGlyphRun> runs = null;
        TextLineBreak previous = null;
        using (var drawing = visual.RenderOpen()) {
            try {
                bool hasText = position < text.Length;
                while (hasText) {
                    using (var formatted = formatter.FormatLine(source, position, 0, paragraph, previous)) {
                        previous?.Dispose();
                        previous = null;
                        bool completeRow = position == 0 && formatted.Length >= text.Length;
                        if (completeRow) {
                            formatted.Draw(drawing, new Point(0, Baseline - formatted.Baseline), InvertAxes.None);
                            return formatted.WidthIncludingTrailingWhitespace;
                        }
                        runs ??= [];
                        runs.AddRange(formatted.GetIndexedGlyphRuns());
                        position += formatted.Length;
                        previous = formatted.GetTextLineBreak();
                    }
                    hasText = position < text.Length;
                }
            }
            finally {
                previous?.Dispose();
            }
            /*
             * Formatter continuations are logical fragments, not visual rows.
             * Restore logical run order, then apply bidi rule L2 over the row.
             */
            runs.Sort((left, right) => left.TextSourceCharacterIndex.CompareTo(right.TextSourceCharacterIndex));
            var levels = LogBidi.GetLevels(text, runs);
            var highestLevel = 0;
            var lowestOddLevel = int.MaxValue;
            foreach (var level in levels) {
                highestLevel = Math.Max(highestLevel, level);
                bool odd = (level & 1) != 0;
                if (odd) {
                    lowestOddLevel = Math.Min(lowestOddLevel, level);
                }
            }
            for (var level = highestLevel; level >= lowestOddLevel; level--) {
                var start = 0;
                for (var i = 0; i <= runs.Count; i++) {
                    bool endsSequence = i == runs.Count || levels[i] < level;
                    if (endsSequence) {
                        runs.Reverse(start, i - start);
                        Array.Reverse(levels, start, i - start);
                        start = i + 1;
                    }
                }
            }
            var x = 0.0;
            foreach (var indexed in runs) {
                var run = indexed.GlyphRun;
                var width = 0.0;
                foreach (var advance in run.AdvanceWidths) {
                    width += advance;
                }
                bool rtl = (run.BidiLevel & 1) != 0;
                var origin = rtl ? x + width : x;
                drawing.PushTransform(new TranslateTransform(origin - run.BaselineOrigin.X, Baseline - run.BaselineOrigin.Y));
                drawing.DrawGlyphRun(brush, run);
                drawing.Pop();
                x += width;
            }
            return x;
        }
    }

    private void Realize() {
        var count = Lines.Count;
        var firstSerial = Lines.FirstSerial;
        var first = firstSerial + Math.Max(0, (int)(VerticalOffset / LineHeight));
        var last = firstSerial + Math.Min(count - 1, (int)((VerticalOffset + ViewportHeight) / LineHeight));
        foreach (var serial in Realized.Keys) {
            bool outside = serial < first || serial > last;
            if (outside) {
                Released.Add(serial);
            }
        }
        foreach (var serial in Released) {
            Reusable.Push(Realized[serial]);
            Realized.Remove(serial);
        }
        Released.Clear();
        bool rebase = Realized.Count == 0 || Math.Abs(first - BaseSerial) > RebaseDistance;
        if (rebase) {
            BaseSerial = first;
            foreach (var pair in Realized) {
                pair.Value.Offset = new Vector(0, (pair.Key - BaseSerial) * LineHeight);
            }
        }
        var width = ExtentWidth;
        using (var formatter = TextFormatter.Create()) {
            for (var serial = first; serial <= last; serial++) {
                bool realized = Realized.ContainsKey(serial);
                if (realized) {
                    continue;
                }
                bool mustCreate = Reusable.Count == 0;
                var visual = mustCreate ? new DrawingVisual() : Reusable.Pop();
                if (mustCreate) {
                    Content.Children.Add(visual);
                }
                width = Math.Max(width, Draw(formatter, visual, Lines[(int)(serial - firstSerial)]));
                visual.Offset = new Vector(0, (serial - BaseSerial) * LineHeight);
                Realized[serial] = visual;
            }
        }
        while (Reusable.Count > 0) {
            Content.Children.Remove(Reusable.Pop());
        }
        bool widthChanged = width != ExtentWidth;
        if (widthChanged) {
            ExtentWidth = width;
            HorizontalOffset = Clamp(HorizontalOffset, MaxHorizontalOffset);
            ScrollOwner?.InvalidateScrollInfo();
        }
        Content.Offset = new Vector(
            Snap(-HorizontalOffset),
            Snap((BaseSerial - firstSerial) * LineHeight - VerticalOffset));
    }

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index) {
        bool indexIsInvalid = index != 0;
        if (indexIsInvalid) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        return Content;
    }

    protected override Size MeasureOverride(Size availableSize) {
        EnsureFont();
        bool widthIsUnbounded = double.IsInfinity(availableSize.Width);
        bool heightIsUnbounded = double.IsInfinity(availableSize.Height);
        MeasuredUnbounded = widthIsUnbounded || heightIsUnbounded;
        return new Size(
            widthIsUnbounded ? ExtentWidth : availableSize.Width,
            heightIsUnbounded ? ExtentHeight : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize) {
        EnsureFont();
        ViewportWidth = finalSize.Width;
        ViewportHeight = finalSize.Height;
        UpdateOffsets();
        Realize();
        return finalSize;
    }

    protected override void OnRender(DrawingContext drawingContext) {
        /*
         * A transparent background makes the whole viewport hit-testable.
         */
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        bool hasSelection = SelectionAnchor.HasValue && Lines.Count > 0;
        if (hasSelection) {
            var first = Math.Max(Lines.FirstSerial, Math.Min(SelectionAnchor.Value, SelectionEnd));
            var last = Math.Min(Lines.AddedCount - 1, Math.Max(SelectionAnchor.Value, SelectionEnd));
            var top = Math.Max(0, (first - Lines.FirstSerial) * LineHeight - VerticalOffset);
            var bottom = Math.Min(RenderSize.Height, (last - Lines.FirstSerial + 1) * LineHeight - VerticalOffset);
            bool visible = bottom > top;
            if (visible) {
                drawingContext.PushOpacity(0.3);
                drawingContext.DrawRectangle(SystemColors.HighlightBrush, null, new Rect(0, top, RenderSize.Width, bottom - top));
                drawingContext.Pop();
            }
        }
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi) {
        base.OnDpiChanged(oldDpi, newDpi);
        InvalidateFont();
    }

    internal void SelectLine(double y, bool extend) {
        bool hasLines = Lines.Count > 0;
        if (hasLines) {
            var index = (int)Clamp(Math.Floor((VerticalOffset + y) / LineHeight), Lines.Count - 1);
            FollowsEnd = false;
            SelectionEnd = Lines.FirstSerial + index;
            bool startsSelection = extend == false || SelectionAnchor.HasValue == false;
            if (startsSelection) {
                SelectionAnchor = SelectionEnd;
            }
            InvalidateVisual();
        }
    }

    internal void ClearSelection() {
        SelectionAnchor = null;
        InvalidateVisual();
    }
    internal string CopyText() {
        var first = Lines.FirstSerial;
        var last = Lines.AddedCount - 1;
        bool hasSelection = SelectionAnchor.HasValue;
        if (hasSelection) {
            first = Math.Max(first, Math.Min(SelectionAnchor.Value, SelectionEnd));
            last = Math.Min(last, Math.Max(SelectionAnchor.Value, SelectionEnd));
        }
        var text = new StringBuilder();
        for (var serial = first; serial <= last; serial++) {
            text.AppendLine(LogLineFormatter.Format(Lines[(int)(serial - Lines.FirstSerial)]));
        }
        return text.ToString();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
        base.OnMouseLeftButtonDown(e);
        Owner.Focus();
        SelectLine(e.GetPosition(this).Y, (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        bool dragging = IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed;
        if (dragging) {
            var y = e.GetPosition(this).Y;
            bool above = y < 0;
            bool below = y > RenderSize.Height;
            if (above) {
                LineUp();
            }
            if (below) {
                LineDown();
            }
            SelectLine(y, true);
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
        base.OnMouseLeftButtonUp(e);
        ReleaseMouseCapture();
        e.Handled = true;
    }

    public LogLineBuffer Lines { get; }
    public int RealizedCount => Realized.Count;

    public bool CanHorizontallyScroll { get; set; }
    public bool CanVerticallyScroll { get; set; }
    public double ExtentWidth { get; private set; }
    public double ExtentHeight { get; private set; }
    public double ViewportWidth { get; private set; }
    public double ViewportHeight { get; private set; }
    public double HorizontalOffset { get; private set; }
    public double VerticalOffset { get; private set; }
    public ScrollViewer ScrollOwner { get; set; }

    public LogPresenter(LogControl owner, int lineLimit) {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Lines = new LogLineBuffer(lineLimit);
        ClipToBounds = true;
        AddVisualChild(Content);
    }

    public void Append(List<LogLine> lines) {
        EnsureFont();
        var removed = 0;
        /*
         * Lines that the buffer would discard within this batch are skipped.
         */
        for (var i = Math.Max(0, lines.Count - Lines.Limit); i < lines.Count; i++) {
            bool replaced = Lines.Add(lines[i]);
            if (replaced) {
                removed++;
            }
        }
        LinesChanged(removed);
    }

    public void SetLineLimit(int limit) {
        var removed = Lines.Resize(limit);
        LinesChanged(removed);
    }

    public void InvalidateFont() {
        FontIsCurrent = false;
        InvalidateMeasure();
        InvalidateArrange();
        InvalidateVisual();
    }

    public void InvalidateLines() {
        ReleaseAll();
        InvalidateArrange();
        InvalidateVisual();
    }

    public void SetHorizontalOffset(double offset) {
        HorizontalOffset = Clamp(offset, MaxHorizontalOffset);
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateArrange();
        InvalidateVisual();
    }

    public void SetVerticalOffset(double offset) {
        VerticalOffset = Clamp(offset, MaxVerticalOffset);
        FollowsEnd = MaxVerticalOffset - VerticalOffset < 1;
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateArrange();
        InvalidateVisual();
    }

    public void LineUp() {
        SetVerticalOffset(VerticalOffset - LineHeight);
    }

    public void LineDown() {
        SetVerticalOffset(VerticalOffset + LineHeight);
    }

    public void LineLeft() {
        SetHorizontalOffset(HorizontalOffset - LineHeight);
    }

    public void LineRight() {
        SetHorizontalOffset(HorizontalOffset + LineHeight);
    }

    public void PageUp() {
        SetVerticalOffset(VerticalOffset - ViewportHeight);
    }

    public void PageDown() {
        SetVerticalOffset(VerticalOffset + ViewportHeight);
    }

    public void PageLeft() {
        SetHorizontalOffset(HorizontalOffset - ViewportWidth);
    }

    public void PageRight() {
        SetHorizontalOffset(HorizontalOffset + ViewportWidth);
    }

    internal static double WheelDistance(int lines, double lineHeight, double viewportHeight) {
        bool pageMode = lines < 0;
        return pageMode ? viewportHeight : Math.Min(viewportHeight, lines * lineHeight);
    }

    public void MouseWheelUp() {
        SetVerticalOffset(VerticalOffset - WheelDistance(SystemParameters.WheelScrollLines, LineHeight, ViewportHeight));
    }

    public void MouseWheelDown() {
        SetVerticalOffset(VerticalOffset + WheelDistance(SystemParameters.WheelScrollLines, LineHeight, ViewportHeight));
    }

    public void MouseWheelLeft() {
        SetHorizontalOffset(HorizontalOffset - 3 * LineHeight);
    }

    public void MouseWheelRight() {
        SetHorizontalOffset(HorizontalOffset + 3 * LineHeight);
    }

    public Rect MakeVisible(Visual visual, Rect rectangle) {
        return rectangle;
    }
}
