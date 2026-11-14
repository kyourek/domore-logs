using Domore.Logs;
using NUnit.Framework;

namespace Domore.Windows.Controls;

[TestFixture]
public sealed class LogFilterTest {
    private sealed class FirstLog {
    }

    private sealed class SecondLog {
    }

    [Test]
    public void UnrestrictedFilterUsesThresholdForEveryType() {
        var filter = new LogFilter(LogSeverity.Warn, null, []);
        Assert.That(filter.ThresholdFor(typeof(FirstLog)), Is.EqualTo(LogSeverity.Warn));
        Assert.That(filter.ThresholdFor(typeof(SecondLog)), Is.EqualTo(LogSeverity.Warn));
    }

    [Test]
    public void ThresholdForExcludedTypeIsNone() {
        var filter = new LogFilter(LogSeverity.Debug, [typeof(FirstLog)], null);
        Assert.That(filter.ThresholdFor(typeof(FirstLog)), Is.EqualTo(LogSeverity.Debug));
        Assert.That(filter.ThresholdFor(typeof(SecondLog)), Is.EqualTo(LogSeverity.None));
    }

    [Test]
    public void ThresholdForNamedTypeUsesTypeName() {
        var filter = new LogFilter(LogSeverity.Info, null, [nameof(SecondLog)]);
        Assert.That(filter.ThresholdFor(typeof(FirstLog)), Is.EqualTo(LogSeverity.None));
        Assert.That(filter.ThresholdFor(typeof(SecondLog)), Is.EqualTo(LogSeverity.Info));
    }

    [Test]
    public void IncludesEntriesMatchingTypeOrName() {
        var filter = new LogFilter(LogSeverity.Info, [typeof(FirstLog)], ["Other"]);
        Assert.That(filter.Includes(LogSeverity.Info, typeof(FirstLog), nameof(FirstLog)), Is.True);
        Assert.That(filter.Includes(LogSeverity.Info, typeof(SecondLog), "Other"), Is.True);
        Assert.That(filter.Includes(LogSeverity.Info, typeof(SecondLog), nameof(SecondLog)), Is.False);
    }

    [Test]
    public void NamesAreCaseSensitive() {
        var filter = new LogFilter(LogSeverity.Info, null, ["firstlog"]);
        Assert.That(filter.Includes(LogSeverity.Info, typeof(FirstLog), nameof(FirstLog)), Is.False);
    }

    [TestCase(LogSeverity.Debug, false)]
    [TestCase(LogSeverity.Info, true)]
    [TestCase(LogSeverity.Critical, true)]
    [TestCase(LogSeverity.None, false)]
    public void IncludesEntriesAtOrAboveThreshold(LogSeverity severity, bool expected) {
        var filter = new LogFilter(LogSeverity.Info, null, null);
        Assert.That(filter.Includes(severity, typeof(FirstLog), nameof(FirstLog)), Is.EqualTo(expected));
    }

    [Test]
    public void ThresholdNoneExcludesEverything() {
        var filter = new LogFilter(LogSeverity.None, null, null);
        Assert.That(filter.ThresholdFor(typeof(FirstLog)), Is.EqualTo(LogSeverity.None));
        Assert.That(filter.Includes(LogSeverity.Critical, typeof(FirstLog), nameof(FirstLog)), Is.False);
    }
}
