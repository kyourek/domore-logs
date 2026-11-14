using Domore.Windows.Controls.Mocks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Domore.Windows.Controls;

[TestFixture]
public sealed class LogStreamingTest {
    private static LogPresenter Presenter(string message, string font = "Consolas") {
        var owner = new LogControl { FontFamily = new FontFamily(font) };
        var lines = new List<LogLine>();
        LogLineFormatter.Split(new MockLogEntry { LogList = [message] }, lines);
        owner.Presenter.Append(lines);
        owner.Presenter.Measure(new Size(600, 100));
        owner.Presenter.Arrange(new Rect(0, 0, 600, 100));
        return owner.Presenter;
    }

    private static IEnumerable<GlyphRun> Runs(Drawing drawing) {
        bool isRun = drawing is GlyphRunDrawing;
        if (isRun) {
            yield return ((GlyphRunDrawing)drawing).GlyphRun;
        }
        bool isGroup = drawing is DrawingGroup;
        if (isGroup) {
            foreach (var child in ((DrawingGroup)drawing).Children) {
                foreach (var run in Runs(child)) {
                    yield return run;
                }
            }
        }
    }

    private static GlyphRun[] Runs(LogPresenter presenter) {
        var container = VisualTreeHelper.GetChild(presenter, 0);
        return Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(container))
            .Select(i => (DrawingVisual)VisualTreeHelper.GetChild(container, i))
            .SelectMany(v => Runs(v.Drawing)).ToArray();
    }

    [TestCase(1000)]
    [TestCase(10000)]
    [TestCase(100000)]
    public void LongUnicodeLineRetainsTrailingMarker(int length) {
        WpfTestThread.Run(() => {
            var presenter = Presenter(new string('x', length) + "😀TAIL");
            var runs = Runs(presenter);
            var characters = string.Concat(runs.Select(r => r.Characters is null ? "" : new string(r.Characters.ToArray())));
            Assert.That(characters, Does.EndWith("😀TAIL"));
            Assert.That(characters.Count(c => c == 'x'), Is.EqualTo(length));
            Assert.That(presenter.ExtentWidth, Is.GreaterThan(length * 4));
            presenter.SetHorizontalOffset(double.PositiveInfinity);
            Assert.That(presenter.HorizontalOffset, Is.EqualTo(presenter.ExtentWidth - presenter.ViewportWidth));
            return Task.CompletedTask;
        });
    }

    [Test]
    public void ArabicUsesWpfShapingAndDirection() {
        WpfTestThread.Run(() => {
            var runs = Runs(Presenter("\u0633\u0644\u0627\u0645", "Arial"));
            var rtl = runs.Where(r => r.BidiLevel % 2 == 1).ToArray();
            Assert.That(rtl, Is.Not.Empty);
            Assert.That(rtl.Sum(r => r.GlyphIndices.Count), Is.EqualTo(3));
            return Task.CompletedTask;
        });
    }

    private static IEnumerable<KeyValuePair<GlyphRun, Matrix>> LocatedRuns(Drawing drawing, Matrix parent) {
        bool isRun = drawing is GlyphRunDrawing;
        if (isRun) {
            yield return new KeyValuePair<GlyphRun, Matrix>(((GlyphRunDrawing)drawing).GlyphRun, parent);
        }
        bool isGroup = drawing is DrawingGroup;
        if (isGroup) {
            var group = (DrawingGroup)drawing;
            var transform = group.Transform?.Value ?? Matrix.Identity;
            transform.Append(parent);
            foreach (var child in group.Children) {
                foreach (var run in LocatedRuns(child, transform)) {
                    yield return run;
                }
            }
        }
    }

    private static double[] CharacterOrigins(LogPresenter presenter, char character) {
        var origins = new List<double>();
        var container = VisualTreeHelper.GetChild(presenter, 0);
        for (var visualIndex = 0; visualIndex < VisualTreeHelper.GetChildrenCount(container); visualIndex++) {
            var visual = (DrawingVisual)VisualTreeHelper.GetChild(container, visualIndex);
            foreach (var located in LocatedRuns(visual.Drawing, Matrix.Identity)) {
                var run = located.Key;
                bool hasCharacters = run.Characters is not null;
                if (hasCharacters) {
                    var advances = new double[run.AdvanceWidths.Count + 1];
                    for (var i = 0; i < run.AdvanceWidths.Count; i++) {
                        advances[i + 1] = advances[i] + run.AdvanceWidths[i];
                    }
                    for (var i = 0; i < run.Characters.Count; i++) {
                        bool matches = run.Characters[i] == character;
                        if (matches) {
                            var cluster = run.ClusterMap is null ? i : run.ClusterMap[i];
                            var direction = run.BidiLevel % 2 == 0 ? 1 : -1;
                            var point = new Point(run.BaselineOrigin.X + direction * advances[cluster], run.BaselineOrigin.Y);
                            origins.Add(located.Value.Transform(point).X);
                        }
                    }
                }
            }
        }
        return origins.ToArray();
    }

    [TestCase(false, 9000)]
    [TestCase(true, 9000)]
    [TestCase(true, 9576)]
    [TestCase(true, 9577)]
    [TestCase(true, 9578)]
    [TestCase(true, 9579)]
    [TestCase(true, 9580)]
    public void LongRtlRunKeepsVisualOrderAcrossContinuations(bool withSpaces, int firstCount) {
        WpfTestThread.Run(() => {
            var separator = withSpaces ? " , " : "";
            var message = "P " + new string('\u05D0', firstCount) + separator +
                new string('\u05D1', 9000) + separator + new string('\u05D2', 9000) + " Z";
            var presenter = Presenter(message, "Arial");
            var first = CharacterOrigins(presenter, '\u05D0');
            var middle = CharacterOrigins(presenter, '\u05D1');
            var last = CharacterOrigins(presenter, '\u05D2');
            Assert.That(first, Has.Length.EqualTo(firstCount));
            Assert.That(middle, Has.Length.EqualTo(9000));
            Assert.That(last, Has.Length.EqualTo(9000));
            Assert.That(last.Max(), Is.LessThan(middle.Min()), "The logical suffix must be left of the middle RTL segment.");
            Assert.That(middle.Max(), Is.LessThan(first.Min()), "The middle RTL segment must be left of the logical prefix.");
            Assert.That(CharacterOrigins(presenter, 'P').Max(), Is.LessThan(last.Min()));
            Assert.That(CharacterOrigins(presenter, 'Z').Min(), Is.GreaterThan(first.Max()));
            return Task.CompletedTask;
        });
    }

    [Test]
    public void LongMixedDirectionGroupsKeepLatinSeparatorBetweenThem() {
        WpfTestThread.Run(() => {
            var message = "P " + new string('\u05D0', 9000) + new string('\u05D1', 9000) +
                " Q " + new string('\u05D2', 9000) + new string('\u05D3', 9000) + " Z";
            var presenter = Presenter(message, "Arial");
            var alef = CharacterOrigins(presenter, '\u05D0');
            var bet = CharacterOrigins(presenter, '\u05D1');
            var gimel = CharacterOrigins(presenter, '\u05D2');
            var dalet = CharacterOrigins(presenter, '\u05D3');
            Assert.That(alef, Has.Length.EqualTo(9000));
            Assert.That(bet, Has.Length.EqualTo(9000));
            Assert.That(gimel, Has.Length.EqualTo(9000));
            Assert.That(dalet, Has.Length.EqualTo(9000));
            Assert.That(bet.Max(), Is.LessThan(alef.Min()));
            Assert.That(dalet.Max(), Is.LessThan(gimel.Min()));
            Assert.That(CharacterOrigins(presenter, 'Q').Min(), Is.GreaterThan(alef.Max()));
            Assert.That(CharacterOrigins(presenter, 'Q').Max(), Is.LessThan(dalet.Min()));
            return Task.CompletedTask;
        });
    }

    [Test]
    public void LongArabicRunKeepsVisualOrderAcrossContinuations() {
        WpfTestThread.Run(() => {
            var message = "P " + new string('\u0628', 9000) + new string('\u062C', 9000) + new string('\u062F', 9000) + " Z";
            var presenter = Presenter(message, "Arial");
            var first = CharacterOrigins(presenter, '\u0628');
            var middle = CharacterOrigins(presenter, '\u062C');
            var last = CharacterOrigins(presenter, '\u062F');
            Assert.That(first, Has.Length.EqualTo(9000));
            Assert.That(middle, Has.Length.EqualTo(9000));
            Assert.That(last, Has.Length.EqualTo(9000));
            Assert.That(last.Max(), Is.LessThan(middle.Min()));
            Assert.That(middle.Max(), Is.LessThan(first.Min()));
            return Task.CompletedTask;
        });
    }

    [TestCase(3)]
    [TestCase(9000)]
    public void NumbersWithinLongRtlTextKeepTheirLeftToRightOrder(int count) {
        WpfTestThread.Run(() => {
            var message = "P " + new string('\u05D0', count) + " 123 " + new string('\u05D1', count) + " Z";
            var presenter = Presenter(message, "Arial");
            var first = CharacterOrigins(presenter, '\u05D0');
            var last = CharacterOrigins(presenter, '\u05D1');
            var one = CharacterOrigins(presenter, '1');
            var two = CharacterOrigins(presenter, '2');
            var three = CharacterOrigins(presenter, '3');
            Assert.That(last.Max(), Is.LessThan(one.Max()));
            Assert.That(one.Max(), Is.LessThan(two.Max()));
            Assert.That(two.Max(), Is.LessThan(three.Max()));
            Assert.That(three.Max(), Is.LessThan(first.Min()));
            return Task.CompletedTask;
        });
    }


    [Test]
    public void ManyAlternatingDirectionRunsPreserveEachRtlPair() {
        WpfTestThread.Run(() => {
            var message = "P " + string.Concat(Enumerable.Repeat("L\u05D0\u05D1 ", 5000)) + " Z";
            var presenter = Presenter(message, "Arial");
            var alef = CharacterOrigins(presenter, '\u05D0');
            var bet = CharacterOrigins(presenter, '\u05D1');
            Assert.That(alef, Has.Length.EqualTo(5000));
            Assert.That(bet, Has.Length.EqualTo(5000));
            for (var i = 0; i < alef.Length; i++) {
                Assert.That(bet[i], Is.LessThan(alef[i]), "Each Hebrew pair must read right to left.");
                bool hasNext = i + 1 < alef.Length;
                if (hasNext) {
                    Assert.That(alef[i], Is.LessThan(bet[i + 1]), "Latin separators must keep the groups in order.");
                }
            }
            return Task.CompletedTask;
        });
    }


    [TestCase(1)]
    [TestCase(2)]
    [TestCase(8)]
    public void NewestLinesMatchForwardFormattingAtEveryBoundary(int limit) {
        string[][] messages = [[], [null], [""], ["\n"], ["a\r\nb\nc\n"], ["a", null, "b\n"]];
        foreach (var message in messages) {
            var entry = new MockLogEntry { LogList = message };
            var all = new List<LogLine>();
            LogLineFormatter.Split(entry, all);
            var newest = new List<LogLine>();
            LogLineFormatter.SplitNewest(entry, newest, limit);
            newest.Reverse();
            Assert.That(newest.Select(LogLineFormatter.Format),
                Is.EqualTo(all.Skip(Math.Max(0, all.Count - limit)).Select(LogLineFormatter.Format)));
        }
    }

    [Test]
    public void MillionLineEntryOnlyStagesConfiguredTail() {
        var lines = new List<LogLine>();
        LogLineFormatter.SplitNewest(new MockLogEntry { LogList = [new string('\n', 1000000) + "last"] }, lines, 2);
        Assert.That(lines, Has.Count.EqualTo(2));
        Assert.That(lines.Capacity, Is.LessThanOrEqualTo(4));
        Assert.That(LogLineFormatter.Format(lines[0]), Does.EndWith("last"));
        Assert.That(lines[1].Length, Is.Zero);
    }

    [Test]
    public void DeliveryYieldsBetweenBoundedBatchesAndPreservesOrder() {
        WpfTestThread.Run(async () => {
            var received = new List<string>();
            var batches = new List<int>();
            var dispatcher = Dispatcher.CurrentDispatcher;
            var subscription = new LogSubscription(dispatcher, entries => {
                batches.Add(entries.Count);
                received.AddRange(entries.Select(e => e.LogName));
            }, new LogFilter(Domore.Logs.LogSeverity.Info, null, null), 200);
            for (var i = 0; i < 200; i++) {
                subscription.Receive(new MockLogEntry { LogName = i.ToString(CultureInfo.InvariantCulture) });
            }
            var observed = -1;
            _ = dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => observed = received.Count));
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.That(observed, Is.EqualTo(64));
            Assert.That(batches, Is.EqualTo(new[] { 64, 64, 64, 8 }));
            Assert.That(received, Is.EqualTo(Enumerable.Range(0, 200).Select(i => i.ToString(CultureInfo.InvariantCulture))));
        });
    }

    [Test]
    public void PendingOverflowKeepsNewestEntries() {
        WpfTestThread.Run(async () => {
            var received = new List<string>();
            var subscription = new LogSubscription(Dispatcher.CurrentDispatcher,
                entries => received.AddRange(entries.Select(e => e.LogName)),
                new LogFilter(Domore.Logs.LogSeverity.Info, null, null), 2);
            subscription.Receive(new MockLogEntry { LogName = "old" });
            subscription.Receive(new MockLogEntry { LogName = "middle" });
            subscription.Receive(new MockLogEntry { LogName = "new" });
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.That(received, Is.EqualTo(new[] { "middle", "new" }));
        });
    }

    [Test]
    public void CopyUsesSelectedRowsAndRetainsTheirIdentityWhenHistoryRolls() {
        WpfTestThread.Run(() => {
            var owner = new LogControl();
            var batch = new List<LogLine>();
            LogLineFormatter.Split(new MockLogEntry { LogList = ["zero\none\ntwo\nthree"] }, batch);
            owner.Presenter.SetLineLimit(4);
            owner.Presenter.Append(batch);
            owner.Presenter.Measure(new Size(600, 100));
            owner.Presenter.Arrange(new Rect(0, 0, 600, 100));
            var height = owner.Presenter.ExtentHeight / 4;
            owner.Presenter.SelectLine(height, false);
            owner.Presenter.SelectLine(2 * height, true);
            Assert.That(owner.Presenter.CopyText(), Does.Contain("one").And.Contain("two").And.Not.Contain("zero").And.Not.Contain("three"));
            batch.Clear();
            LogLineFormatter.Split(new MockLogEntry { LogList = ["four"] }, batch);
            owner.Presenter.Append(batch);
            Assert.That(owner.Presenter.CopyText(), Does.Contain("one").And.Contain("two").And.Not.Contain("four"));
            owner.Presenter.ClearSelection();
            Assert.That(owner.Presenter.CopyText(), Does.Contain("one").And.Contain("two").And.Contain("three").And.Contain("four"));
            return Task.CompletedTask;
        });
    }
    [TestCase(-1, 100)]
    [TestCase(0, 0)]
    [TestCase(3, 48)]
    [TestCase(20, 100)]
    public void WheelDistanceRespectsPageModeAndViewport(int lines, double expected) {
        Assert.That(LogPresenter.WheelDistance(lines, 16, 100), Is.EqualTo(expected));
    }
    [Test, Explicit("Manual end-to-end rendering workload; no timing assertion.")]
    public void MeasureStreamingViewportWorkload() {
        WpfTestThread.Run(() => {
            var owner = new LogControl();
            var batch = new List<LogLine>();
            for (var i = 0; i < 64; i++) {
                LogLineFormatter.Split(new MockLogEntry { LogList = ["Streaming log " + i + " café 日本語 😀"] }, batch);
            }
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < 1000; i++) {
                owner.Presenter.Append(batch);
                owner.Presenter.Measure(new Size(1000, 600));
                owner.Presenter.Arrange(new Rect(0, 0, 1000, 600));
            }
            watch.Stop();
            TestContext.Out.WriteLine("64000 lines / 1000 viewport updates: " + watch.ElapsedMilliseconds + " ms; realized=" + owner.Presenter.RealizedCount);
            Assert.That(owner.Presenter.Lines.Count, Is.EqualTo(10000));
            Assert.That(owner.Presenter.RealizedCount, Is.LessThan(50));
            return Task.CompletedTask;
        });
    }
}
