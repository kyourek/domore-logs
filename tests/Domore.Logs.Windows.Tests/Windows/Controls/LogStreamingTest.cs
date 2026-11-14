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
