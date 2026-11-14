using Domore.Logs;
using Domore.Windows.Controls.Mocks;
using NUnit.Framework;
using System.Linq;

namespace Domore.Windows.Controls;

[TestFixture]
public sealed class LogLineFormatterTest {
    private static LogLineBuffer Split(MockLogEntry entry, int limit = 100) {
        var lines = new LogLineBuffer(limit);
        LogLineFormatter.Split(entry, lines);
        return lines;
    }

    private static string[] Format(MockLogEntry entry, int limit = 100) {
        var lines = Split(entry, limit);
        return Enumerable.Range(0, lines.Count).Select(i => LogLineFormatter.Format(lines[i])).ToArray();
    }

    [Test]
    public void SingleMessageFollowsHeader() {
        var entry = new MockLogEntry { LogSeverity = LogSeverity.Warn, LogList = ["message"] };
        Assert.That(Format(entry), Is.EqualTo(new[] { "01:02:03.045 wrn Mock message" }));
    }

    [Test]
    public void SingleMessageLinesEachFollowHeader() {
        var entry = new MockLogEntry { LogList = ["one\r\ntwo\nthree"] };
        Assert.That(Format(entry), Is.EqualTo(new[] {
            "01:02:03.045 inf Mock one",
            "01:02:03.045 inf Mock two",
            "01:02:03.045 inf Mock three"
        }));
    }

    [Test]
    public void MultipleMessagesAreIndentedBelowHeader() {
        var entry = new MockLogEntry { LogSeverity = LogSeverity.Debug, LogList = ["one", "two\nthree"] };
        Assert.That(Format(entry), Is.EqualTo(new[] {
            "01:02:03.045 dbg Mock",
            "  one",
            "  two",
            "  three"
        }));
    }

    [Test]
    public void NoMessagesWritesHeaderOnly() {
        var entry = new MockLogEntry();
        Assert.That(Format(entry), Is.EqualTo(new[] { "01:02:03.045 inf Mock" }));
    }

    [Test]
    public void TabsAndControlCharactersAreReplaced() {
        var entry = new MockLogEntry { LogList = ["a\tb\u0001c\rd"] };
        Assert.That(Format(entry), Is.EqualTo(new[] { "01:02:03.045 inf Mock a    b c d" }));
    }

    [Test]
    public void LineAndParagraphSeparatorsAreReplaced() {
        var message = "before" + (char)0x2028 + "middle" + (char)0x2029 + "after";
        var entry = new MockLogEntry { LogList = [message] };
        Assert.That(Format(entry), Is.EqualTo(new[] { "01:02:03.045 inf Mock before middle after" }));
    }

    [Test]
    public void EmptyAndNullMessagesProduceEmptyLines() {
        var entry = new MockLogEntry { LogList = ["", null] };
        Assert.That(Format(entry), Is.EqualTo(new[] { "01:02:03.045 inf Mock", "  ", "  " }));
    }

    [Test]
    public void SplitKeepsNewestLinesWithinBufferLimit() {
        var entry = new MockLogEntry { LogList = ["one", "two", "three" + (char)10 + "four"] };
        Assert.That(Format(entry, limit: 2), Is.EqualTo(new[] { "  three", "  four" }));
    }

    [Test]
    public void WriteGrowsBufferForLongLines() {
        var message = new string('x', 5000);
        var entry = new MockLogEntry { LogList = [message] };
        var lines = Split(entry);
        var buffer = new char[8];
        var length = LogLineFormatter.Write(lines[0], ref buffer);
        Assert.That(new string(buffer, 0, length), Is.EqualTo("01:02:03.045 inf Mock " + message));
    }
}
