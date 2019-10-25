using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs {
    [TestFixture]
    public class LoggerTest {
        private Logger Subject;

        private class LogHandler : ILogHandler {
            public LogSeverity Severity { get; set; }
            public string Format { get; set; }
            public IList<string> Entries { get; } = new List<string>();
            public void Handle(string message, LogSeverity severity) => Entries.Add(message);
        }

        private void SetSeverity(LogSeverity value) {
            var handler = new LogHandler { Severity = value };
            Subject.AddHandler(handler);
        }

        [SetUp]
        public void SetUp() {
            Subject = (Logger)Logging.For(typeof(LoggerTest));
            Subject.GetHandlers().ToList().ForEach(handler => Subject.RemoveHandler(handler));
        }

        [TearDown]
        public void TearDown() {
            Logging.Complete();
            Logging.Reset();
        }

        [Test]
        public void Severity_IsNoneByDefault() {
            Assert.AreEqual(LogSeverity.None, Subject.Severity);
        }

        [Test]
        public void Name_IsFullNameOfType() {
            var actual = Subject.Name;
            var expected = Subject.Type.FullName;
            Assert.AreEqual(expected, actual);
        }

        [TestCase(LogSeverity.Debug, true)]
        [TestCase(LogSeverity.Info, false)]
        [TestCase(LogSeverity.Warn, false)]
        [TestCase(LogSeverity.Error, false)]
        [TestCase(LogSeverity.Critical, false)]
        [TestCase(LogSeverity.None, false)]
        public void Severity_ChangesDebug(LogSeverity value, bool expected) {
            SetSeverity(value);
            var actual = Subject.Debug();
            Assert.AreEqual(expected, actual);
        }

        [TestCase(LogSeverity.Debug, true)]
        [TestCase(LogSeverity.Info, true)]
        [TestCase(LogSeverity.Warn, false)]
        [TestCase(LogSeverity.Error, false)]
        [TestCase(LogSeverity.Critical, false)]
        [TestCase(LogSeverity.None, false)]
        public void Severity_ChangesInfo(LogSeverity value, bool expected) {
            SetSeverity(value);
            var actual = Subject.Info();
            Assert.AreEqual(expected, actual);
        }

        [TestCase(LogSeverity.Debug, true)]
        [TestCase(LogSeverity.Info, true)]
        [TestCase(LogSeverity.Warn, true)]
        [TestCase(LogSeverity.Error, false)]
        [TestCase(LogSeverity.Critical, false)]
        [TestCase(LogSeverity.None, false)]
        public void Severity_ChangesWarn(LogSeverity value, bool expected) {
            SetSeverity(value);
            var actual = Subject.Warn();
            Assert.AreEqual(expected, actual);
        }

        [TestCase(LogSeverity.Debug, true)]
        [TestCase(LogSeverity.Info, true)]
        [TestCase(LogSeverity.Warn, true)]
        [TestCase(LogSeverity.Error, true)]
        [TestCase(LogSeverity.Critical, false)]
        [TestCase(LogSeverity.None, false)]
        public void Severity_ChangesError(LogSeverity value, bool expected) {
            SetSeverity(value);
            var actual = Subject.Error();
            Assert.AreEqual(expected, actual);
        }

        [TestCase(LogSeverity.Debug, true)]
        [TestCase(LogSeverity.Info, true)]
        [TestCase(LogSeverity.Warn, true)]
        [TestCase(LogSeverity.Error, true)]
        [TestCase(LogSeverity.Critical, true)]
        [TestCase(LogSeverity.None, false)]
        public void Severity_ChangesCritical(LogSeverity value, bool expected) {
            SetSeverity(value);
            var actual = Subject.Critical();
            Assert.AreEqual(expected, actual);
        }


        [TestCase(LogSeverity.Debug, LogSeverity.Debug)]
        [TestCase(LogSeverity.Debug, LogSeverity.Info)]
        [TestCase(LogSeverity.Debug, LogSeverity.Warn)]
        [TestCase(LogSeverity.Debug, LogSeverity.Error)]
        [TestCase(LogSeverity.Debug, LogSeverity.Critical)]
        [TestCase(LogSeverity.Info, LogSeverity.Info)]
        [TestCase(LogSeverity.Info, LogSeverity.Warn)]
        [TestCase(LogSeverity.Info, LogSeverity.Error)]
        [TestCase(LogSeverity.Info, LogSeverity.Critical)]
        [TestCase(LogSeverity.Warn, LogSeverity.Warn)]
        [TestCase(LogSeverity.Warn, LogSeverity.Error)]
        [TestCase(LogSeverity.Warn, LogSeverity.Critical)]
        [TestCase(LogSeverity.Error, LogSeverity.Error)]
        [TestCase(LogSeverity.Error, LogSeverity.Critical)]
        [TestCase(LogSeverity.Critical, LogSeverity.Critical)]
        public void Entry_CallsHandlerIfSeverityIsSufficient(LogSeverity handlerSeverity, LogSeverity entrySeverity) {
            var handler = new LogHandler();
            handler.Severity = handlerSeverity;
            handler.Format = "{Data}";

            Subject.AddHandler(handler);
            Subject.Entry(entrySeverity, "the log");

            Assert.That(handler.Entries, Is.EqualTo(new[] { "the log" }));
        }

        [TestCase(LogSeverity.Debug, LogSeverity.None)]
        [TestCase(LogSeverity.Info, LogSeverity.Debug)]
        [TestCase(LogSeverity.Warn, LogSeverity.Debug)]
        [TestCase(LogSeverity.Warn, LogSeverity.Info)]
        [TestCase(LogSeverity.Error, LogSeverity.Debug)]
        [TestCase(LogSeverity.Error, LogSeverity.Info)]
        [TestCase(LogSeverity.Error, LogSeverity.Warn)]
        [TestCase(LogSeverity.Critical, LogSeverity.Debug)]
        [TestCase(LogSeverity.Critical, LogSeverity.Info)]
        [TestCase(LogSeverity.Critical, LogSeverity.Warn)]
        [TestCase(LogSeverity.Critical, LogSeverity.Error)]
        public void Entry_DoesNotCallHandlerIfSeverityIsInsufficient(LogSeverity handlerSeverity, LogSeverity entrySeverity) {
            var handler = new LogHandler();
            handler.Severity = handlerSeverity;

            Subject.Entry(entrySeverity, "the log");
            Assert.That(handler.Entries.Count, Is.Zero);
        }

        [TestCase("{Data}", "the log", "the log")]
        [TestCase(" \t{Data} \n", "should trim", "should trim")]
        [TestCase("{Severity} {Data}", "show me Severity", "Info show me Severity")]
        [TestCase("[{Severity}] {Data}", "put brackets around severity", "[Info] put brackets around severity")]
        [TestCase("{Severity} {Severity}", "forget the data", "Info Info")]
        [TestCase("[{Severity}] {Data} {{{Name}}}", "Show me the name.", "[Info] Show me the name. {Domore.Logs.LoggerTest}")]
        public void Info_CallsHandlerWithFormattedEntry(string format, string data, string expected) {
            var handler = new LogHandler();
            handler.Severity = LogSeverity.Info;
            handler.Format = format;
            Subject.AddHandler(handler);
            Subject.Info(data);
            Assert.That(handler.Entries[0], Is.EqualTo(expected));
        }
    }
}
