using Domore.Logs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Domore.Windows.Controls;

/*
 * Retains one DrawingVisual per visible line inside a container visual.
 * Scrolling and following new lines move the container, and only lines that
 * enter the viewport are drawn; their visuals are reused from lines that left
 * it. Realization runs during arrange, so it happens at most once per layout
 * pass however many changes occur in between.
 *
 * Lines are drawn as glyph runs built from the font's glyph map, bypassing
 * text layout. A line containing a character the font cannot map is drawn
 * with FormattedText so that font fallback applies.
 *
 * Line visuals are positioned relative to a base serial near the viewport,
 * which keeps coordinates small however many lines have been added.
 */
internal sealed class LogPresenter : FrameworkElement, IScrollInfo {
    private const int MaxGlyphsPerRun = 1024;
    private const int MaxCharactersPerPiece = 4096;
    private const char ZeroWidthJoiner = (char)0x200D;
    private const long RebaseDistance = 10000;

    private readonly LogControl Owner;
    private readonly ContainerVisual Content = new();
    private readonly Dictionary<long, DrawingVisual> Realized = [];
    private readonly List<long> Released = [];
    private readonly Stack<DrawingVisual> Reusable = new();

    private char[] TextBuffer = new char[256];
    private ushort[] IndexBuffer = new ushort[256];
    private double[] AdvanceBuffer = new double[256];
    private bool FontIsCurrent;
    private bool FollowsEnd = true;
    private bool MeasuredUnbounded;
    private long OffsetFirstSerial;
    private long BaseSerial;
    private Typeface Typeface;
    private LogGlyphTable Glyphs;
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

    private static T[] Fit<T>(T[] buffer, int length) {
        bool fits = buffer.Length >= length;
        return fits
            ? buffer
            : new T[Math.Max(length, 2 * buffer.Length)];
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
        bool hasGlyphTypeface = Typeface.TryGetGlyphTypeface(out var glyphTypeface);
        Glyphs = hasGlyphTypeface
            ? new LogGlyphTable(glyphTypeface, FontEmSize)
            : null;
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

    private void LinesChanged() {
        var removed = Lines.FirstSerial - OffsetFirstSerial;
        OffsetFirstSerial = Lines.FirstSerial;
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
    }

    private Brush GetBrush(LogSeverity severity) {
        return Owner.GetSeverityForeground(severity) ?? Brushes.Transparent;
    }

    private bool DrawGlyphs(DrawingContext drawingContext, int length, Brush brush, out double width) {
        width = 0;
        IndexBuffer = Fit(IndexBuffer, length);
        AdvanceBuffer = Fit(AdvanceBuffer, length);
        for (var i = 0; i < length; i++) {
            bool unmapped = Glyphs.TryGet(TextBuffer[i], out IndexBuffer[i], out AdvanceBuffer[i]) == false;
            if (unmapped) {
                return false;
            }
        }
        /*
         * Long lines are split into several runs, each with its own arrays
         * because a glyph run keeps the lists it is given.
         */
        var x = 0.0;
        for (var start = 0; start < length; start += MaxGlyphsPerRun) {
            var count = Math.Min(MaxGlyphsPerRun, length - start);
            var indices = new ushort[count];
            var advances = new double[count];
            Array.Copy(IndexBuffer, start, indices, 0, count);
            Array.Copy(AdvanceBuffer, start, advances, 0, count);
            var glyphRun = new GlyphRun(
                glyphTypeface: Glyphs.Typeface,
                bidiLevel: 0,
                isSideways: false,
                renderingEmSize: FontEmSize,
                pixelsPerDip: (float)PixelsPerDip,
                glyphIndices: indices,
                baselineOrigin: new Point(x, Baseline),
                advanceWidths: advances,
                glyphOffsets: null,
                characters: null,
                deviceFontName: null,
                clusterMap: null,
                caretStops: null,
                language: null);
            drawingContext.DrawGlyphRun(brush, glyphRun);
            for (var i = 0; i < count; i++) {
                x += advances[i];
            }
        }
        width = x;
        return true;
    }

    private double Draw(DrawingVisual visual, LogLine line) {
        var length = LogLineFormatter.Write(line, ref TextBuffer);
        var brush = GetBrush(line.Severity);
        using (var drawingContext = visual.RenderOpen()) {
            var width = 0.0;
            bool drawnAsGlyphs = Glyphs is not null && DrawGlyphs(drawingContext, length, brush, out width);
            if (drawnAsGlyphs) {
                return width;
            }
            return DrawFormatted(drawingContext, length, brush);
        }
    }

    private static bool CanBreakBetween(char previous, char next) {
        bool splitsPair = char.IsLowSurrogate(next);
        bool followsJoiner = previous == ZeroWidthJoiner;
        var category = char.GetUnicodeCategory(next);
        bool continuesCluster =
            category == UnicodeCategory.NonSpacingMark ||
            category == UnicodeCategory.SpacingCombiningMark ||
            category == UnicodeCategory.EnclosingMark ||
            category == UnicodeCategory.Format;
        return splitsPair == false && followsJoiner == false && continuesCluster == false;
    }

    /*
     * Ends a piece after whitespace near the limit when possible, otherwise
     * at the latest position that does not split a character or cluster.
     */
    private int FindPieceEnd(int start, int length) {
        var limit = start + MaxCharactersPerPiece;
        bool fits = limit >= length;
        if (fits) {
            return length;
        }
        for (var end = limit; end > start + MaxCharactersPerPiece / 2; end--) {
            bool afterWhitespace = char.IsWhiteSpace(TextBuffer[end - 1]);
            if (afterWhitespace) {
                return end;
            }
        }
        for (var end = limit; end > start; end--) {
            bool canBreak = CanBreakBetween(TextBuffer[end - 1], TextBuffer[end]);
            if (canBreak) {
                return end;
            }
        }
        return limit;
    }

    /*
     * WPF breaks a line of text after about 9,600 characters, so long text is
     * formatted in pieces placed side by side. Each piece is aligned to the
     * line's baseline, which also aligns fallback fonts with glyph-run lines.
     */
    private double DrawFormatted(DrawingContext drawingContext, int length, Brush brush) {
        var x = 0.0;
        var start = 0;
        var more = start < length;
        while (more) {
            var end = FindPieceEnd(start, length);
            var formattedText = new FormattedText(
                new string(TextBuffer, start, end - start),
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                Typeface,
                FontEmSize,
                brush,
                PixelsPerDip) {
                MaxLineCount = 1
            };
            drawingContext.DrawText(formattedText, new Point(x, Baseline - formattedText.Baseline));
            x += formattedText.WidthIncludingTrailingWhitespace;
            start = end;
            more = start < length;
        }
        return x;
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
            width = Math.Max(width, Draw(visual, Lines[(int)(serial - firstSerial)]));
            visual.Offset = new Vector(0, (serial - BaseSerial) * LineHeight);
            Realized[serial] = visual;
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
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi) {
        base.OnDpiChanged(oldDpi, newDpi);
        InvalidateFont();
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
        /*
         * Text is drawn in left-to-right coordinates, so an inherited right-to-left
         * flow direction would mirror it. The surrounding template still follows
         * the application's flow direction.
         */
        FlowDirection = FlowDirection.LeftToRight;
        AddVisualChild(Content);
    }

    /*
     * Called after lines are added to Lines. Lines discarded in the meantime
     * are found from the change in the first serial.
     */
    public void LinesAdded() {
        EnsureFont();
        LinesChanged();
    }

    public void SetLineLimit(int limit) {
        Lines.Resize(limit);
        LinesChanged();
    }

    public void InvalidateFont() {
        FontIsCurrent = false;
        InvalidateMeasure();
        InvalidateArrange();
    }

    public void InvalidateLines() {
        ReleaseAll();
        InvalidateArrange();
    }

    public void SetHorizontalOffset(double offset) {
        HorizontalOffset = Clamp(offset, MaxHorizontalOffset);
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateArrange();
    }

    public void SetVerticalOffset(double offset) {
        VerticalOffset = Clamp(offset, MaxVerticalOffset);
        FollowsEnd = MaxVerticalOffset - VerticalOffset < 1;
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateArrange();
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

    /*
     * A negative count is the system's one-screen-at-a-time setting, and a
     * count of more lines than fit in the viewport also scrolls one page.
     */
    public double GetWheelDistance(int wheelScrollLines) {
        bool scrollsPage = wheelScrollLines < 0;
        if (scrollsPage) {
            return ViewportHeight;
        }
        return Math.Min(wheelScrollLines * LineHeight, ViewportHeight);
    }

    public void MouseWheelUp() {
        SetVerticalOffset(VerticalOffset - GetWheelDistance(SystemParameters.WheelScrollLines));
    }

    public void MouseWheelDown() {
        SetVerticalOffset(VerticalOffset + GetWheelDistance(SystemParameters.WheelScrollLines));
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
