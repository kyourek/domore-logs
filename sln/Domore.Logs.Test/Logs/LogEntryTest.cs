using NUnit.Framework;
using System;
using System.Text;

namespace Domore.Logs {
    [TestFixture]
    public class LogEntryTest {
        [Test]
        public void ToString_TimeIsInDefaultFormat() {
            var now = new DateTime(2016, 6, 13, 14, 7, 8, 21);
            var entry = new LogEntry(now, LogSeverity.Info, null);
            var message = entry.ToString();
            Assert.IsTrue(message.Contains("2016-06-13 14:07:08"));
        }

        [Test]
        [TestCase(LogSeverity.Debug)]
        [TestCase(LogSeverity.Info)]
        [TestCase(LogSeverity.Warn)]
        [TestCase(LogSeverity.Error)]
        [TestCase(LogSeverity.Critical)]
        public void ToString_UsesSeverity(LogSeverity value) {
            var now = new DateTime(2008, 11, 5, 6, 33, 57, 101);
            var entry = new LogEntry(now, value, null);
            var message = entry.ToString();
            Assert.AreEqual("2008-11-05 06:33:57 [" + value + "] ", message);
        }

        [Test]
        public void ToString_SplitsDataWithNewLine() {
            var now = new DateTime(2008, 11, 5, 6, 33, 57, 101);
            var entry = new LogEntry(now, LogSeverity.Info, new object[] { "The value is", 7 });
            var message = entry.ToString();
            Assert.AreEqual("2008-11-05 06:33:57 [Info] The value is" + Environment.NewLine + "7", message);
        }

        [Test]
        public void Create_SetsLogSeverityForErrors() {
            var entry = LogEntry.Create("msg", new ArgumentNullException(), null);
            Assert.AreEqual(LogSeverity.Error, entry.Severity);
        }

        [Test]
        public void Create_SetsLogSeverityForInfo() {
            var entry = LogEntry.Create("msg", null, null);
            Assert.AreEqual(LogSeverity.Info, entry.Severity);
        }

        [Test]
        public void DataStrings_ContainsStringsOfDataWhenConstructed() {
            var sb = new StringBuilder();
            sb.Append("Hello, World!");
            var entry = new LogEntry(LogSeverity.Info, new[] { sb });
            sb.Clear();
            sb.Append("Greetings earth.");
            Assert.AreEqual(1, entry.DataStrings.Length);
            Assert.AreEqual("Hello, World!", entry.DataStrings[0]);
            Assert.AreEqual(1, entry.Data.Length);
            Assert.AreEqual("Greetings earth.", entry.Data[0].ToString());
        }
    }
}
