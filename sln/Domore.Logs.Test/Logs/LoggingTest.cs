using NUnit.Framework;
using System;
using System.Linq;

namespace Domore.Logs {
    using Handlers;

    [TestFixture]
    public class LoggingTest {
        private class LogFileConfiguration : ILogConfiguration {
            public LogSeverity Severity { get; set; }
            public string Name { get; set; }
            public TimeSpan ClearInterval { get; set; }

            public void Configure(object obj, string key = null) {
                if (obj is LogFile logFile) {
                    logFile.Name = Name;
                    logFile.Severity = Severity;
                    logFile.ClearInterval = ClearInterval;
                }
            }
        }

        [SetUp]
        public void SetUp() {
            Logging.Reset();
            Logging.Handler.Clear();
            Logging.Handler["log"] = "logfile";
            Logging.Configuration = null;
        }

        [TearDown]
        public void TearDown() {
            Logging.Complete();
        }

        [Test]
        public void For_AddsLogFileHandler() {
            var log = (Logger)Logging.For(typeof(LoggingTest));
            var file = log.GetHandlers().OfType<LogFile>().SingleOrDefault();
            Assert.IsNotNull(file);
        }

        //[Test]
        //public void For_AddsLogMailHandler() {
        //    var log = (Logger)Logging.For(typeof(LoggingTest));
        //    var mail = log.GetHandlers().OfType<LogMail>().SingleOrDefault();
        //    Assert.IsNotNull(mail);
        //}

        [Test]
        public void For_SetsLogFileSeverity() {
            var log = (Logger)Logging.For(typeof(LoggingTest));
            var file = log.GetHandlers().OfType<LogFile>().First();
            Assert.AreEqual(LogSeverity.None, file.Severity);
        }

        //[Test]
        //public void For_SetsLogMailSeverity() {
        //    var log = (Logger)Logging.For(typeof(LoggingTest));
        //    var mail = log.GetHandlers().OfType<LogMail>().First();
        //    Assert.AreEqual(LogSeverity.Critical, mail.Severity);
        //}

        [TestCase(LogSeverity.Debug)]
        [TestCase(LogSeverity.Info)]
        [TestCase(LogSeverity.Warn)]
        [TestCase(LogSeverity.Error)]
        [TestCase(LogSeverity.Critical)]
        [TestCase(LogSeverity.None)]
        public void For_SetsFileSeverityFromConfiguration(LogSeverity severity) {
            Logging.Configuration = new LogFileConfiguration { Severity = severity };

            var log = (Logger)Logging.For(typeof(LoggingTest));
            var file = log.GetHandlers().OfType<LogFile>().First();

            Assert.AreEqual(severity, file.Severity);
        }

        [TestCase("the-only-log.txt")]
        public void For_SetsFileNameGloballyFromConfiguration(string filename) {
            Logging.Configuration = new LogFileConfiguration { Name = filename };

            var log = (Logger)Logging.For(typeof(LoggingTest));
            var file = log.GetHandlers().OfType<LogFile>().First();

            Assert.AreEqual(filename, file.Name);
        }

        //[TestCase(LogSeverity.Debug)]
        //[TestCase(LogSeverity.Info)]
        //[TestCase(LogSeverity.Warn)]
        //[TestCase(LogSeverity.Error)]
        //[TestCase(LogSeverity.Critical)]
        //[TestCase(LogSeverity.None)]
        //public void For_SetsMailSeverityFromConfiguration(LogSeverity severity) {
        //    Logging.Configuration.Content = "Domore.Logs.LoggingTest.mail.Severity = " + severity;

        //    var log = (Logger)Logging.For(typeof(LoggingTest));
        //    var mail = log.GetHandlers().OfType<LogMail>().First();

        //    Assert.AreEqual(severity, mail.Severity);
        //}

        //[TestCase("kyourek@domore.com")]
        //[TestCase("gcorral@domore.com")]
        //public void For_SetsMailToFromConfiguration(string to) {
        //    Logging.Configuration.Content = "Domore.Logs.LoggingTest.mail.To = " + to;

        //    var log = (Logger)Logging.For(typeof(LoggingTest));
        //    var mail = log.GetHandlers().OfType<LogMail>().First();

        //    Assert.AreEqual(to, mail.To);
        //}

        //[TestCase("recipient@logs.org")]
        //public void For_SetsMailToGloballyFromConfiguration(string to) {
        //    Logging.Configuration.Content = "LogMail.To = " + to;

        //    var log = (Logger)Logging.For(typeof(LoggingTest));
        //    var mail = log.GetHandlers().OfType<LogMail>().First();

        //    Assert.AreEqual(to, mail.To);
        //}

        [TestCase("7")]
        [TestCase("12:00:00")]
        public void For_SetsFileClearIntervalFromConfiguration(string clearInterval) {
            Logging.Configuration = new LogFileConfiguration { ClearInterval = TimeSpan.Parse(clearInterval) };
            var logger = (Logger)Logging.For(typeof(LoggerTest));
            var handler = logger.GetHandlers().OfType<LogFile>().Single();
            var actual = handler.ClearInterval;
            var expected = TimeSpan.Parse(clearInterval);
            Assert.AreEqual(expected, actual);
        }
    }
}
