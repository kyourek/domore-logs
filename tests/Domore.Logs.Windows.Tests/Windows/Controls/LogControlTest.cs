using Domore.Logs;
using Domore.Windows.Controls.Mocks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Domore.Windows.Controls;

[TestFixture]
public sealed class LogControlTest {
    private static readonly ILog FirstLog = Logging.For(typeof(FirstType));
    private static readonly ILog SecondLog = Logging.For(typeof(SecondType));
    private static readonly ILog ThirdLog = Logging.For(typeof(ThirdType));

    private sealed class FirstType {
    }

    private sealed class SecondType {
    }

    private sealed class ThirdType {
    }

    /*
     * Pending entries are flushed at background priority, which runs before
     * the context-idle continuation of this yield.
     */
    private static async Task<string[]> Lines(LogControl control) {
        await Dispatcher.Yield(DispatcherPriority.ContextIdle);
        var lines = control.Presenter.Lines;
        return Enumerable.Range(0, lines.Count).Select(i => LogLineFormatter.Format(lines[i])).ToArray();
    }

    private static string[] Messages(string[] lines) {
        return lines.Select(line => line.Substring(line.IndexOf(' ') + 1)).ToArray();
    }

    private static LogControl Attached(Action<LogControl> configure = null) {
        var control = new LogControl();
        configure?.Invoke(control);
        Assert.That(control.Attach(), Is.True);
        return control;
    }

    private static IEnumerable<GlyphRun> GlyphRuns(Drawing drawing) {
        var glyphRunDrawing = drawing as GlyphRunDrawing;
        bool isGlyphRun = glyphRunDrawing is not null;
        if (isGlyphRun) {
            yield return glyphRunDrawing.GlyphRun;
        }
        var group = drawing as DrawingGroup;
        bool isGroup = group is not null;
        if (isGroup) {
            foreach (var child in group.Children) {
                foreach (var glyphRun in GlyphRuns(child)) {
                    yield return glyphRun;
                }
            }
        }
    }

    /*
     * Returns the glyph runs of every realized line, in drawing order.
     */
    private static GlyphRun[] GlyphRuns(LogControl control) {
        var content = VisualTreeHelper.GetChild(control.Presenter, 1);
        return Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(content))
            .Select(i => VisualTreeHelper.GetChild(content, i))
            .SelectMany(line => GlyphRuns(VisualTreeHelper.GetDrawing((Visual)line)))
            .ToArray();
    }

    private static void Layout(LogControl control, double width, double height) {
        bool initialized = control.IsInitialized;
        if (initialized == false) {
            control.BeginInit();
            control.EndInit();
        }
        control.ApplyTemplate();
        control.Measure(new Size(width, height));
        control.Arrange(new Rect(0, 0, width, height));
        control.UpdateLayout();
    }

    private static Style HiddenScrollBars() {
        var style = new Style(typeof(ScrollViewer));
        style.Setters.Add(new Setter(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden));
        style.Setters.Add(new Setter(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden));
        return style;
    }

    private static int[] Pixels(FrameworkElement root, int width, int height) {
        root.Measure(new Size(width, height));
        root.Arrange(new Rect(0, 0, width, height));
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var pixels = new int[width * height];
        bitmap.CopyPixels(pixels, width * 4, 0);
        return pixels;
    }

    /*
     * Renders one long log line, scrolled horizontally, inside a parent with
     * the given flow direction. Scroll bars are hidden because they follow the
     * flow direction by design.
     */
    private static async Task<int[]> RenderLongLine(FlowDirection flowDirection, double horizontalOffset) {
        var control = Attached(c => {
            c.Background = Brushes.White;
            c.Foreground = Brushes.Black;
            c.LogScrollViewerStyle = HiddenScrollBars();
        });
        try {
            var root = new Border { FlowDirection = flowDirection, Child = control };
            control.BeginInit();
            control.EndInit();
            FirstLog.Info(string.Concat(Enumerable.Repeat("ABCDEFG readable 123 ", 15)));
            await Lines(control);
            Pixels(root, 400, 40);
            control.Presenter.SetHorizontalOffset(horizontalOffset);
            return Pixels(root, 400, 40);
        }
        finally {
            control.Detach();
        }
    }

    private static int White {
        get {
            return unchecked((int)0xFFFFFFFF);
        }
    }

    /*
     * Returns the share of pixels that are not background in either image and
     * are the same in both.
     */
    private static double Agreement(int[] first, int[] second) {
        var ink = 0;
        var same = 0;
        for (var i = 0; i < first.Length; i++) {
            bool background = first[i] == White && second[i] == White;
            if (background) {
                continue;
            }
            ink++;
            bool equal = first[i] == second[i];
            if (equal) {
                same++;
            }
        }
        return ink == 0 ? 0 : (double)same / ink;
    }

    private sealed class CaptureSubscription : ILogSubscription {
        public WeakReference Entry { get; private set; }
        public WeakReference Text { get; private set; }

        public event EventHandler ThresholdChanged {
            add { }
            remove { }
        }

        public LogSeverity Threshold(Type type) {
            return LogSeverity.Info;
        }

        public void Receive(ILogEntry entry) {
            Entry = new WeakReference(entry);
            Text = new WeakReference(entry.LogList.First());
        }
    }

    /*
     * Logs a message while observing its entry and text weakly. These helpers
     * are not inlined so that no local keeps the message alive.
     */
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Lined(int count, int width) {
        return string.Join(((char)10).ToString(), Enumerable.Range(0, count).Select(i => i.ToString().PadLeft(width, '.')));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CaptureSubscription LogCaptured(int count, int width) {
        var capture = new CaptureSubscription();
        Assert.That(Logging.Subscribe(capture), Is.True);
        FirstLog.Info(Lined(count, width));
        Logging.Unsubscribe(capture);
        return capture;
    }

    private static string[] CopiedMessages(LogControl control) {
        var lines = control.Presenter.CopyText().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        return Messages(lines);
    }

    private static async Task<LogControl> Scrollable(int count) {
        var control = Attached();
        Layout(control, 400, 100);
        for (var i = 0; i < count; i++) {
            FirstLog.Info(i);
        }
        await Lines(control);
        control.UpdateLayout();
        return control;
    }

    private static double LineHeightOf(LogPresenter presenter) {
        return presenter.ExtentHeight / presenter.Lines.Count;
    }

    private static void Collect() {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [TearDown]
    public void TearDown() {
        Logging.Complete();
    }

    [Test]
    public void DisplaysEntriesAtOrAboveThreshold() {
        WpfTestThread.Run(async () => {
            var control = Attached(c => c.LogThreshold = LogSeverity.Warn);
            FirstLog.Info("info");
            FirstLog.Warn("warn");
            FirstLog.Critical("critical");
            var lines = await Lines(control);
            Assert.That(Messages(lines), Is.EqualTo(new[] {
                "wrn FirstType warn",
                "crt FirstType critical"
            }));
        });
    }

    [Test]
    public void DefaultThresholdIsInfo() {
        WpfTestThread.Run(async () => {
            var control = Attached();
            FirstLog.Debug("debug");
            FirstLog.Info("info");
            var lines = await Lines(control);
            Assert.That(control.LogThreshold, Is.EqualTo(LogSeverity.Info));
            Assert.That(Messages(lines), Is.EqualTo(new[] { "inf FirstType info" }));
        });
    }

    [Test]
    public void ThresholdNoneDisplaysNothing() {
        WpfTestThread.Run(async () => {
            var control = Attached(c => c.LogThreshold = LogSeverity.None);
            FirstLog.Critical("critical");
            Assert.That(await Lines(control), Is.Empty);
        });
    }

    [Test]
    public void ChangingThresholdAppliesToLaterEntries() {
        WpfTestThread.Run(async () => {
            var control = Attached(c => c.LogThreshold = LogSeverity.Error);
            FirstLog.Info("before");
            control.LogThreshold = LogSeverity.Debug;
            FirstLog.Debug("after");
            var lines = await Lines(control);
            Assert.That(Messages(lines), Is.EqualTo(new[] { "dbg FirstType after" }));
        });
    }

    [Test]
    public void LogTypesRestrictDisplayedEntries() {
        WpfTestThread.Run(async () => {
            var control = Attached(c => c.LogTypes = [typeof(SecondType)]);
            FirstLog.Info("first");
            SecondLog.Info("second");
            var lines = await Lines(control);
            Assert.That(Messages(lines), Is.EqualTo(new[] { "inf SecondType second" }));
        });
    }

    [Test]
    public void LogNamesRestrictDisplayedEntries() {
        WpfTestThread.Run(async () => {
            var control = Attached(c => c.LogNames = [nameof(FirstType)]);
            FirstLog.Info("first");
            SecondLog.Info("second");
            var lines = await Lines(control);
            Assert.That(Messages(lines), Is.EqualTo(new[] { "inf FirstType first" }));
        });
    }

    [Test]
    public void LogTypesAndLogNamesAreCombined() {
        WpfTestThread.Run(async () => {
            var control = Attached(c => {
                c.LogTypes = [typeof(FirstType)];
                c.LogNames = [nameof(SecondType)];
            });
            FirstLog.Info("first");
            SecondLog.Info("second");
            ThirdLog.Info("third");
            var lines = await Lines(control);
            Assert.That(Messages(lines), Is.EqualTo(new[] {
                "inf FirstType first",
                "inf SecondType second"
            }));
        });
    }

    [Test]
    public void ObservableLogNamesChangesAreApplied() {
        WpfTestThread.Run(async () => {
            var names = new ObservableCollection<string> { nameof(FirstType) };
            var control = Attached(c => c.LogNames = names);
            SecondLog.Info("excluded");
            names.Add(nameof(SecondType));
            SecondLog.Info("included");
            var lines = await Lines(control);
            Assert.That(Messages(lines), Is.EqualTo(new[] { "inf SecondType included" }));
        });
    }

    [Test]
    public void ReplacedObservableLogNamesAreNoLongerObserved() {
        WpfTestThread.Run(async () => {
            var names = new ObservableCollection<string> { nameof(FirstType) };
            var control = Attached(c => c.LogNames = names);
            control.LogNames = [nameof(FirstType)];
            names.Add(nameof(SecondType));
            SecondLog.Info("excluded");
            Assert.That(await Lines(control), Is.Empty);
        });
    }

    [Test]
    public void LineLimitDiscardsOldestLines() {
        WpfTestThread.Run(async () => {
            var control = Attached(c => c.LogLineLimit = 3);
            for (var i = 0; i < 10; i++) {
                FirstLog.Info(i);
            }
            var lines = await Lines(control);
            Assert.That(Messages(lines), Is.EqualTo(new[] {
                "inf FirstType 7",
                "inf FirstType 8",
                "inf FirstType 9"
            }));
        });
    }

    [Test]
    public void LoweringLineLimitDiscardsOldestLines() {
        WpfTestThread.Run(async () => {
            var control = Attached();
            for (var i = 0; i < 5; i++) {
                FirstLog.Info(i);
            }
            await Lines(control);
            control.LogLineLimit = 2;
            var lines = await Lines(control);
            Assert.That(Messages(lines), Is.EqualTo(new[] {
                "inf FirstType 3",
                "inf FirstType 4"
            }));
        });
    }

    [Test]
    public void LineLimitMustBePositive() {
        WpfTestThread.Run(() => {
            var control = new LogControl();
            Assert.That(() => control.LogLineLimit = 0, Throws.ArgumentException);
            return Task.CompletedTask;
        });
    }

    [Test]
    public void MultipleMessagesAreIndentedBelowHeader() {
        WpfTestThread.Run(async () => {
            var control = Attached();
            FirstLog.Info("one", "two\tthree\r\nfour");
            var lines = await Lines(control);
            Assert.That(Messages(lines), Is.EqualTo(new[] {
                "inf FirstType",
                " one",
                " two    three",
                " four"
            }));
        });
    }

    [Test]
    public void EntriesLoggedOnWorkerThreadsAreDisplayed() {
        WpfTestThread.Run(async () => {
            var control = Attached();
            await Task.WhenAll(Enumerable.Range(0, 4).Select(i => Task.Run(() => {
                for (var j = 0; j < 25; j++) {
                    FirstLog.Info(i);
                }
            })));
            var lines = await Lines(control);
            Assert.That(lines, Has.Length.EqualTo(100));
        });
    }

    [Test]
    public void DetachedControlDoesNotDisplayEntries() {
        WpfTestThread.Run(async () => {
            var control = Attached();
            Assert.That(control.Detach(), Is.True);
            FirstLog.Info("detached");
            Assert.That(await Lines(control), Is.Empty);
        });
    }

    [Test]
    public void ViewFollowsNewLinesWhileScrolledToEnd() {
        WpfTestThread.Run(async () => {
            var control = Attached();
            Layout(control, 400, 100);
            for (var i = 0; i < 100; i++) {
                FirstLog.Info(i);
            }
            await Lines(control);
            Layout(control, 400, 100);
            var presenter = control.Presenter;
            Assert.That(presenter.VerticalOffset, Is.GreaterThan(0));
            Assert.That(presenter.VerticalOffset, Is.EqualTo(presenter.ExtentHeight - presenter.ViewportHeight));
        });
    }

    [Test]
    public void ViewKeepsItsLinesWhileScrolledAway() {
        WpfTestThread.Run(async () => {
            var control = Attached(c => c.LogLineLimit = 100);
            Layout(control, 400, 100);
            for (var i = 0; i < 100; i++) {
                FirstLog.Info(i);
            }
            await Lines(control);
            var presenter = control.Presenter;
            var lineHeight = presenter.ExtentHeight / presenter.Lines.Count;
            presenter.SetVerticalOffset(50 * lineHeight);
            for (var i = 0; i < 10; i++) {
                FirstLog.Info(i);
            }
            await Lines(control);
            Assert.That(presenter.VerticalOffset, Is.EqualTo(40 * lineHeight).Within(0.001));
        });
    }

    [Test]
    public void OnlyVisibleLinesAreRealized() {
        WpfTestThread.Run(async () => {
            var control = Attached();
            Layout(control, 400, 100);
            for (var i = 0; i < 1000; i++) {
                FirstLog.Info(i);
            }
            await Lines(control);
            control.UpdateLayout();
            var presenter = control.Presenter;
            var lineHeight = presenter.ExtentHeight / presenter.Lines.Count;
            var visibleLines = (int)Math.Ceiling(presenter.ViewportHeight / lineHeight) + 1;
            Assert.That(presenter.RealizedCount, Is.InRange(1, visibleLines));
            presenter.SetVerticalOffset(0);
            control.UpdateLayout();
            Assert.That(presenter.RealizedCount, Is.InRange(1, visibleLines));
        });
    }

    [Test]
    public void ChangedForegroundRedrawsRealizedLines() {
        WpfTestThread.Run(async () => {
            var control = Attached();
            FirstLog.Info("text");
            await Lines(control);
            Layout(control, 400, 100);
            Assert.That(control.Presenter.RealizedCount, Is.EqualTo(1));
            control.LogInfoForeground = Brushes.Red;
            Assert.That(control.Presenter.RealizedCount, Is.EqualTo(0));
            control.UpdateLayout();
            Assert.That(control.Presenter.RealizedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ComplexScriptIsShapedWhenFontMapsEveryCharacter() {
        WpfTestThread.Run(async () => {
            var control = Attached(c => c.FontFamily = new FontFamily("Arial"));
            var typeface = new Typeface(control.FontFamily, control.FontStyle, control.FontWeight, control.FontStretch);
            bool hasGlyphTypeface = typeface.TryGetGlyphTypeface(out var glyphTypeface);
            if (hasGlyphTypeface == false) {
                Assert.Ignore("Arial is not installed.");
            }
            var word = new string(new[] { (char)0x0633, (char)0x0644, (char)0x0627, (char)0x0645 });
            Assert.That(word.All(c => glyphTypeface.CharacterToGlyphMap.ContainsKey(c)), Is.True);
            FirstLog.Info(word);
            await Lines(control);
            Layout(control, 400, 100);
            var runs = GlyphRuns(control);
            Assert.That(runs.Any(run => run.BidiLevel % 2 == 1), Is.True, "The Arabic text was not laid out right to left.");
        });
    }

    /*
     * Returns the width of a line holding text. The entry has a fixed date, so
     * lines differ only in their text and not in their header's time.
     */
    private static double WidthOf(string text) {
        var control = new LogControl { FontFamily = new FontFamily("Arial") };
        LogLineFormatter.Split(new MockLogEntry { LogList = [text] }, control.Presenter.Lines);
        control.Presenter.LinesAdded();
        Layout(control, 600, 100);
        return control.Presenter.ExtentWidth;
    }

    [Test]
    public void SoftHyphenAddsNoVisibleHyphenOrWidth() {
        WpfTestThread.Run(() => {
            var soft = string.Concat("pass", (char)0xAD, "word");
            var typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            if (typeface.TryGetGlyphTypeface(out _) == false) {
                Assert.Ignore("Arial is not installed.");
            }
            var plain = WidthOf("password");
            var withSoftHyphen = WidthOf(soft);
            var withHyphen = WidthOf("pass-word");
            Assert.That(withHyphen, Is.GreaterThan(plain + 2), "A visible hyphen should add width.");
            Assert.That(withSoftHyphen, Is.EqualTo(plain).Within(1), "The soft hyphen was drawn.");
            return Task.CompletedTask;
        });
    }

    private static readonly string HebrewWord = new(new[] { (char)0x05D0, (char)0x05D1, (char)0x05D2 });
    private static readonly string FinalHebrewWord = new(new[] { (char)0x05E9, (char)0x05DC, (char)0x05D5, (char)0x05DD });

    private static string HebrewPhrase(int words) {
        return string.Join(" ", Enumerable.Repeat(HebrewWord, words)) + " " + FinalHebrewWord;
    }

    private static string Describe(IEnumerable<GlyphRun> runs) {
        return string.Join(" | ", runs.Select(run => string.Format(
            CultureInfo.InvariantCulture,
            "x={0:F2} level={1} glyphs={2}",
            run.BaselineOrigin.X,
            run.BidiLevel,
            run.GlyphIndices.Count)));
    }

    private static LogControl ArialAttached() {
        var typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        bool hasArial = typeface.TryGetGlyphTypeface(out _);
        if (hasArial == false) {
            Assert.Ignore("Arial is not installed.");
        }
        return Attached(c => c.FontFamily = new FontFamily("Arial"));
    }

    [Test]
    public void LongRightToLeftPassageIsOrderedLikeTheUnsplitLine() {
        WpfTestThread.Run(async () => {
            var control = ArialAttached();
            FirstLog.Info(HebrewPhrase(1250));
            await Lines(control);
            Layout(control, 600, 100);
            var text = LogLineFormatter.Format(control.Presenter.Lines[0]);
            Assert.That(text.Length, Is.GreaterThan(5000).And.LessThan(9000));
            Assert.That(Describe(GlyphRuns(control)), Is.EqualTo(Describe(UnsplitRuns(control, text))));
        });
    }

    /*
     * Returns the glyph runs of text formatted once by WPF, as a reference for
     * the pieces the presenter draws for a long line.
     */
    private static GlyphRun[] UnsplitRuns(LogControl control, string text) {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(control.FontFamily, control.FontStyle, control.FontWeight, control.FontStretch),
            control.FontSize,
            Brushes.Black,
            VisualTreeHelper.GetDpi(control.Presenter).PixelsPerDip) {
            MaxLineCount = 1
        };
        var reference = new DrawingGroup();
        using (var drawingContext = reference.Open()) {
            drawingContext.DrawText(formattedText, new Point(0, 0));
        }
        return GlyphRuns(reference).ToArray();
    }

    /*
     * The line starts with a header of 27 characters, so a prefix of 8,163
     * characters puts the break between the base emoji and its modifier.
     */
    [TestCase(8161)]
    [TestCase(8163)]
    [TestCase(8165)]
    public void EmojiModifierStaysWithItsBaseAtAPieceBoundary(int prefix) {
        WpfTestThread.Run(async () => {
            var typeface = new Typeface(new FontFamily("Segoe UI Emoji"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            if (typeface.TryGetGlyphTypeface(out _) == false) {
                Assert.Ignore("Segoe UI Emoji is not installed.");
            }
            var control = Attached(c => c.FontFamily = new FontFamily("Segoe UI Emoji"));
            FirstLog.Info(new string('x', prefix) + char.ConvertFromUtf32(0x1F44D) + char.ConvertFromUtf32(0x1F3FD) + "END");
            await Lines(control);
            Layout(control, 600, 100);
            var text = LogLineFormatter.Format(control.Presenter.Lines[0]);
            Assert.That(text.Length, Is.InRange(8192, 9500));
            var glyphs = GlyphRuns(control).Sum(run => run.GlyphIndices.Count);
            var unsplit = UnsplitRuns(control, text).Sum(run => run.GlyphIndices.Count);
            Assert.That(glyphs, Is.EqualTo(unsplit), "The modifier was shaped apart from its base.");
        });
    }

    /*
     * A right-to-left phrase reads from right to left, so the run holding its
     * last word must be drawn left of every other run of the phrase.
     */
    [Test]
    public void RightToLeftPhraseNearAPieceBreakKeepsItsOrder() {
        WpfTestThread.Run(async () => {
            var control = ArialAttached();
            var before = string.Concat(Enumerable.Repeat("lorem ", 1290));
            var after = string.Concat(Enumerable.Repeat(" ipsum", 300));
            FirstLog.Info(before + HebrewPhrase(249) + after + char.ConvertFromUtf32(0x1F600));
            await Lines(control);
            Layout(control, 600, 100);
            Assert.That(LogLineFormatter.Format(control.Presenter.Lines[0]).Length, Is.GreaterThan(9600));
            var rightToLeft = GlyphRuns(control).Where(run => run.BidiLevel % 2 == 1).ToArray();
            var last = rightToLeft.Single(run => run.Characters.Contains(FinalHebrewWord[0]));
            var others = rightToLeft.Where(run => run != last).ToArray();
            Assert.That(others, Is.Not.Empty);
            Assert.That(others.Select(run => run.BaselineOrigin.X), Is.All.GreaterThan(last.BaselineOrigin.X));
        });
    }

    [Test]
    public void LongFallbackLineIsDrawnInFullOnOneRow() {
        WpfTestThread.Run(async () => {
            var control = Attached();
            FirstLog.Info(new string('x', 10000) + char.ConvertFromUtf32(0x1F600) + "END");
            await Lines(control);
            Layout(control, 400, 100);
            var runs = GlyphRuns(control);
            var text = string.Concat(runs.SelectMany(run => run.Characters ?? []));
            Assert.That(text.Count(c => c == 'x'), Is.EqualTo(10000));
            Assert.That(text, Does.EndWith("END"));
            Assert.That(runs.Select(run => Math.Round(run.BaselineOrigin.Y, 3)).Distinct().Count(), Is.EqualTo(1));
            Assert.That(control.Presenter.Lines.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void TextAfterLineSeparatorIsDrawnOnTheSameRow() {
        WpfTestThread.Run(async () => {
            var control = Attached();
            FirstLog.Info("before" + (char)0x2028 + "after" + char.ConvertFromUtf32(0x1F600) + "END");
            await Lines(control);
            Layout(control, 400, 100);
            var runs = GlyphRuns(control);
            var text = string.Concat(runs.SelectMany(run => run.Characters ?? []));
            Assert.That(text, Does.Contain("before").And.EndWith("END"));
            Assert.That(runs.Select(run => Math.Round(run.BaselineOrigin.Y, 3)).Distinct().Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public void LargeMultilineEntryKeepsNewestLinesWithoutStagingEveryLine() {
        WpfTestThread.Run(async () => {
            var control = Attached(c => c.LogLineLimit = 2);
            FirstLog.Info(string.Join("\n", Enumerable.Range(0, 200000)));
#if NETCOREAPP
            var allocated = GC.GetAllocatedBytesForCurrentThread();
#endif
            var lines = await Lines(control);
#if NETCOREAPP
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Assert.That(allocated, Is.LessThan(1024 * 1024), "Delivery allocated memory for discarded lines.");
#endif
            Assert.That(lines, Is.EqualTo(new[] {
                "  199998",
                "  199999"
            }));
        });
    }

    [Test]
    public void PageModeWheelScrollsOneViewportInWheelDirection() {
        WpfTestThread.Run(async () => {
            var control = Attached();
            Layout(control, 400, 100);
            for (var i = 0; i < 100; i++) {
                FirstLog.Info(i);
            }
            await Lines(control);
            control.UpdateLayout();
            var presenter = control.Presenter;
            var lineHeight = presenter.ExtentHeight / presenter.Lines.Count;
            Assert.That(presenter.GetWheelDistance(-1), Is.EqualTo(presenter.ViewportHeight), "Page mode");
            Assert.That(presenter.GetWheelDistance(3), Is.EqualTo(3 * lineHeight).Within(0.001), "Three lines");
            Assert.That(presenter.GetWheelDistance(1000), Is.EqualTo(presenter.ViewportHeight), "More lines than fit");
            Assert.That(presenter.GetWheelDistance(0), Is.EqualTo(0), "No lines");
        });
    }

    [TestCase(0.0)]
    [TestCase(150.0)]
    public void TextIsNotMirroredInRightToLeftParent(double horizontalOffset) {
        WpfTestThread.Run(async () => {
            var leftToRight = await RenderLongLine(FlowDirection.LeftToRight, horizontalOffset);
            var rightToLeft = await RenderLongLine(FlowDirection.RightToLeft, horizontalOffset);
            Assert.That(leftToRight.Count(pixel => pixel != White), Is.GreaterThan(100), "Nothing was drawn.");
            Assert.That(Agreement(leftToRight, rightToLeft), Is.GreaterThan(0.8));
        });
    }

    [Test]
    public void DiscardedLinesDoNotKeepSourceEntryAlive() {
        WpfTestThread.Run(async () => {
            var control = Attached(c => c.LogLineLimit = 2);
            var capture = LogCaptured(20000, 50);
            var lines = await Lines(control);
            Collect();
            Assert.That(capture.Entry.IsAlive, Is.False, "The entry is kept by a buffered line.");
            Assert.That(capture.Text.IsAlive, Is.False, "A discarded message is kept by a buffered line.");
            Assert.That(lines, Is.EqualTo(new[] {
                "  " + "19998".PadLeft(50, '.'),
                "  " + "19999".PadLeft(50, '.')
            }));
        });
    }

    [Test]
    public void BufferedLinesDoNotKeepSourceEntryAlive() {
        WpfTestThread.Run(async () => {
            var control = Attached();
            var capture = LogCaptured(3, 100000);
            var lines = await Lines(control);
            Collect();
            Assert.That(capture.Entry.IsAlive, Is.False, "The entry is kept by a buffered line.");
            Assert.That(lines.Skip(1), Is.EqualTo(new[] {
                "  " + "0".PadLeft(100000, '.'),
                "  " + "1".PadLeft(100000, '.'),
                "  " + "2".PadLeft(100000, '.')
            }));
        });
    }

    [Test]
    public void CopyUsesSelectedRowsAndKeepsThemAsHistoryRolls() {
        WpfTestThread.Run(async () => {
            var control = Attached(c => c.LogLineLimit = 4);
            foreach (var text in new[] { "zero", "one", "two", "three" }) {
                FirstLog.Info(text);
            }
            await Lines(control);
            Layout(control, 600, 100);
            var presenter = control.Presenter;
            var lineHeight = LineHeightOf(presenter);
            presenter.SelectLine(1.5 * lineHeight, extend: false);
            presenter.SelectLine(2.5 * lineHeight, extend: true);
            Assert.That(CopiedMessages(control), Is.EqualTo(new[] { "inf FirstType one", "inf FirstType two" }));
            FirstLog.Info("four");
            await Lines(control);
            Assert.That(CopiedMessages(control), Is.EqualTo(new[] { "inf FirstType one", "inf FirstType two" }));
            FirstLog.Info("five");
            FirstLog.Info("six");
            await Lines(control);
            Assert.That(CopiedMessages(control), Is.Empty, "The selected rows were discarded.");
            Assert.That(presenter.ClearSelection(), Is.True);
            Assert.That(CopiedMessages(control), Is.EqualTo(new[] {
                "inf FirstType three",
                "inf FirstType four",
                "inf FirstType five",
                "inf FirstType six"
            }));
        });
    }

    [Test]
    public void SelectingPausesFollowingAndEndResumesIt() {
        WpfTestThread.Run(async () => {
            var control = await Scrollable(100);
            var presenter = control.Presenter;
            Assert.That(presenter.VerticalOffset, Is.EqualTo(presenter.ExtentHeight - presenter.ViewportHeight));
            presenter.SelectLine(10, extend: false);
            var paused = presenter.VerticalOffset;
            for (var i = 0; i < 10; i++) {
                FirstLog.Info(i);
            }
            await Lines(control);
            control.UpdateLayout();
            Assert.That(presenter.VerticalOffset, Is.EqualTo(paused));
            Assert.That(presenter.VerticalOffset, Is.LessThan(presenter.ExtentHeight - presenter.ViewportHeight));
            Assert.That(control.HandleKey(Key.End), Is.True);
            for (var i = 0; i < 10; i++) {
                FirstLog.Info(i);
            }
            await Lines(control);
            control.UpdateLayout();
            Assert.That(presenter.VerticalOffset, Is.EqualTo(presenter.ExtentHeight - presenter.ViewportHeight));
        });
    }

    [Test]
    public void EscapeIsUsedOnlyToClearASelection() {
        WpfTestThread.Run(async () => {
            var control = await Scrollable(3);
            Assert.That(control.HandleKey(Key.Escape), Is.False);
            control.Presenter.SelectLine(0, extend: false);
            Assert.That(CopiedMessages(control), Has.Length.EqualTo(1));
            Assert.That(control.HandleKey(Key.Escape), Is.True);
            Assert.That(CopiedMessages(control), Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void SelectionHighlightCoversSelectedRowsAndMovesWithScrolling() {
        WpfTestThread.Run(async () => {
            var control = await Scrollable(50);
            var presenter = control.Presenter;
            presenter.SetVerticalOffset(0);
            control.UpdateLayout();
            var lineHeight = LineHeightOf(presenter);
            presenter.SelectLine(2.5 * lineHeight, extend: false);
            presenter.SelectLine(3.5 * lineHeight, extend: true);
            control.UpdateLayout();
            var highlight = (DrawingVisual)VisualTreeHelper.GetChild(presenter, 0);
            Assert.That(highlight.ContentBounds.Top, Is.EqualTo(2 * lineHeight).Within(0.5));
            Assert.That(highlight.ContentBounds.Height, Is.EqualTo(2 * lineHeight).Within(0.5));
            presenter.SetVerticalOffset(lineHeight);
            control.UpdateLayout();
            Assert.That(highlight.ContentBounds.Top, Is.EqualTo(lineHeight).Within(0.5));
            presenter.ClearSelection();
            control.UpdateLayout();
            Assert.That(highlight.ContentBounds.IsEmpty, Is.True);
        });
    }

    /*
     * Returns the brush and the effective opacity that the first drawing in
     * the selection visual's content is drawn with.
     */
    private static (Brush Brush, double Opacity) SelectionLook(Drawing drawing, double opacity = 1) {
        var geometry = drawing as GeometryDrawing;
        bool isGeometry = geometry is not null;
        if (isGeometry) {
            return (geometry.Brush, opacity);
        }
        var group = drawing as DrawingGroup;
        bool isGroup = group is not null;
        if (isGroup) {
            foreach (var child in group.Children) {
                var look = SelectionLook(child, opacity * group.Opacity);
                bool found = look.Brush is not null;
                if (found) {
                    return look;
                }
            }
        }
        return (null, opacity);
    }

    private static Color ColorOf(Brush brush) {
        return ((SolidColorBrush)brush).Color;
    }

    [Test]
    public void SelectionBackgroundDefaultsToTheDimmedSystemHighlightAndFollowsTheProperty() {
        WpfTestThread.Run(async () => {
            var control = await Scrollable(3);
            var highlight = (DrawingVisual)VisualTreeHelper.GetChild(control.Presenter, 0);
            control.Presenter.SelectLine(0, extend: false);
            control.UpdateLayout();
            var look = SelectionLook(VisualTreeHelper.GetDrawing(highlight));
            Assert.That(control.LogSelectionBackground, Is.Null);
            Assert.That(ColorOf(look.Brush), Is.EqualTo(SystemColors.HighlightColor));
            Assert.That(look.Opacity, Is.EqualTo(0.3).Within(0.001));
            control.LogSelectionBackground = new SolidColorBrush(Color.FromArgb(0x80, 0x11, 0x22, 0x33));
            control.UpdateLayout();
            look = SelectionLook(VisualTreeHelper.GetDrawing(highlight));
            Assert.That(ColorOf(look.Brush), Is.EqualTo(Color.FromArgb(0x80, 0x11, 0x22, 0x33)));
            Assert.That(look.Opacity, Is.EqualTo(1).Within(0.001), "A set brush is drawn as given.");
            control.LogSelectionBackground = null;
            control.UpdateLayout();
            look = SelectionLook(VisualTreeHelper.GetDrawing(highlight));
            Assert.That(ColorOf(look.Brush), Is.EqualTo(SystemColors.HighlightColor));
            Assert.That(look.Opacity, Is.EqualTo(0.3).Within(0.001));
        });
    }

    [Test]
    public void KeysScrollTheView() {
        WpfTestThread.Run(async () => {
            var control = await Scrollable(100);
            var presenter = control.Presenter;
            var lineHeight = LineHeightOf(presenter);
            Assert.That(control.HandleKey(Key.Home), Is.True);
            Assert.That(presenter.VerticalOffset, Is.EqualTo(0));
            Assert.That(control.HandleKey(Key.PageDown), Is.True);
            Assert.That(presenter.VerticalOffset, Is.EqualTo(presenter.ViewportHeight));
            Assert.That(control.HandleKey(Key.Down), Is.True);
            Assert.That(presenter.VerticalOffset, Is.EqualTo(presenter.ViewportHeight + lineHeight).Within(0.001));
            Assert.That(control.HandleKey(Key.Up), Is.True);
            Assert.That(control.HandleKey(Key.PageUp), Is.True);
            Assert.That(presenter.VerticalOffset, Is.EqualTo(0).Within(0.001));
            Assert.That(control.HandleKey(Key.End), Is.True);
            Assert.That(presenter.VerticalOffset, Is.EqualTo(presenter.ExtentHeight - presenter.ViewportHeight));
            Assert.That(control.HandleKey(Key.A), Is.False);
        });
    }

    [Test]
    public void CopyCommandIsAvailableAndControlTakesFocus() {
        WpfTestThread.Run(() => {
            var control = new LogControl();
            Assert.That(ApplicationCommands.Copy.CanExecute(null, control), Is.True);
            Assert.That(control.Focusable, Is.True);
            return Task.CompletedTask;
        });
    }

    private static void LayoutPanel(StackPanel panel) {
        panel.Measure(new Size(400, 100));
        panel.Arrange(new Rect(0, 0, 400, 100));
        panel.UpdateLayout();
    }

    /*
     * In a horizontal StackPanel the control is measured without a width limit,
     * so it must size itself to the text its presenter realizes while arranging.
     */
    private static void AssertSizedToText(LogControl control) {
        var presenter = control.Presenter;
        Assert.That(presenter.ExtentWidth, Is.GreaterThan(0), "No text was realized.");
        Assert.That(presenter.ViewportWidth, Is.EqualTo(presenter.ExtentWidth).Within(0.5));
        Assert.That(control.ActualWidth, Is.GreaterThanOrEqualTo(presenter.ExtentWidth));
    }

    [Test]
    public void ControlWithHiddenScrollBarsSizesToItsTextInUnboundedWidth() {
        WpfTestThread.Run(async () => {
            var control = Attached(c => c.LogScrollViewerStyle = HiddenScrollBars());
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(control);
            control.BeginInit();
            control.EndInit();
            FirstLog.Info("A short line");
            await Lines(control);
            LayoutPanel(panel);
            AssertSizedToText(control);
            var narrow = control.Presenter.ExtentWidth;
            FirstLog.Info(new string('M', 70));
            await Lines(control);
            LayoutPanel(panel);
            AssertSizedToText(control);
            Assert.That(control.Presenter.ExtentWidth, Is.GreaterThan(narrow), "A wider line did not widen the control.");
            var small = control.Presenter.ExtentWidth;
            control.FontSize = 24;
            LayoutPanel(panel);
            AssertSizedToText(control);
            Assert.That(control.Presenter.ExtentWidth, Is.GreaterThan(small), "A larger font did not widen the control.");
        });
    }

    [Test]
    public void RendersGlyphAndFallbackText() {
        WpfTestThread.Run(async () => {
            var control = Attached();
            FirstLog.Info("plain text");
            FirstLog.Error("fallback 日本語 😀");
            await Lines(control);
            Layout(control, 400, 100);
            var bitmap = new RenderTargetBitmap(400, 100, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(control);
            var pixels = new int[400 * 100];
            bitmap.CopyPixels(pixels, 400 * 4, 0);
            Assert.That(pixels.Distinct().Count(), Is.GreaterThan(2));
            Assert.That(control.Presenter.ExtentWidth, Is.GreaterThan(0));
        });
    }
}
