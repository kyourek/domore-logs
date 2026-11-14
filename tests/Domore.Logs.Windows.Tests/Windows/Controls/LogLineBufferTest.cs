using Domore.Windows.Controls.Mocks;
using NUnit.Framework;
using System;
using System.Linq;

namespace Domore.Windows.Controls;

[TestFixture]
public sealed class LogLineBufferTest {
    private static readonly MockLogEntry Entry = new();

    private static LogLine Line(string text) {
        return new LogLine(Entry, text, 0, text.Length, LogLineKind.Indented);
    }

    private static string[] Texts(LogLineBuffer buffer) {
        return Enumerable.Range(0, buffer.Count).Select(i => buffer[i].Message).ToArray();
    }

    private static LogLineBuffer Fill(int limit, int count) {
        var buffer = new LogLineBuffer(limit);
        for (var i = 0; i < count; i++) {
            buffer.Add(Line(i.ToString()));
        }
        return buffer;
    }

    [Test]
    public void AddKeepsLinesInOrderBelowLimit() {
        var buffer = Fill(limit: 5, count: 3);
        Assert.That(Texts(buffer), Is.EqualTo(new[] { "0", "1", "2" }));
    }

    [Test]
    public void AddDiscardsOldestLineAtLimit() {
        var buffer = Fill(limit: 3, count: 7);
        Assert.That(Texts(buffer), Is.EqualTo(new[] { "4", "5", "6" }));
    }

    [Test]
    public void AddReportsWhetherALineWasDiscarded() {
        var buffer = Fill(limit: 2, count: 2);
        Assert.That(buffer.Add(Line("2")), Is.True);
        Assert.That(Fill(limit: 3, count: 2).Add(Line("2")), Is.False);
    }

    [Test]
    public void ResizeDiscardsOldestLines() {
        var buffer = Fill(limit: 5, count: 8);
        var removed = buffer.Resize(2);
        Assert.That(removed, Is.EqualTo(3));
        Assert.That(Texts(buffer), Is.EqualTo(new[] { "6", "7" }));
    }

    [Test]
    public void ResizeLargerKeepsLinesAndGrows() {
        var buffer = Fill(limit: 3, count: 5);
        buffer.Resize(4);
        buffer.Add(Line("5"));
        buffer.Add(Line("6"));
        Assert.That(Texts(buffer), Is.EqualTo(new[] { "3", "4", "5", "6" }));
    }

    [Test]
    public void ClearRemovesLines() {
        var buffer = Fill(limit: 3, count: 5);
        buffer.Clear();
        buffer.Add(Line("a"));
        Assert.That(Texts(buffer), Is.EqualTo(new[] { "a" }));
    }

    [Test]
    public void SerialsSurviveDiscardedLines() {
        var buffer = Fill(limit: 3, count: 7);
        Assert.That(buffer.AddedCount, Is.EqualTo(7));
        Assert.That(buffer.FirstSerial, Is.EqualTo(4));
        buffer.Resize(1);
        Assert.That(buffer.FirstSerial, Is.EqualTo(6));
        buffer.Clear();
        Assert.That(buffer.FirstSerial, Is.EqualTo(7));
    }

    [Test]
    public void LimitMustBePositive() {
        Assert.That(() => new LogLineBuffer(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => new LogLineBuffer(1).Resize(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }
}
