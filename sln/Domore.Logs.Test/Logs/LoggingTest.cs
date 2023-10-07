using NUnit.Framework;
using System;
using System.Linq;

namespace Domore.Logs {
    [TestFixture]
    public sealed class LoggingTest {
        private class TestService : ILogService {
            void ILogService.Log(string name, string data, LogSeverity severity) {
                throw new NotImplementedException();
            }
        }

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

        [Test]
        public void For_SetsLogFileSeverity() {
            var log = (Logger)Logging.For(typeof(LoggingTest));
            var file = log.GetHandlers().OfType<LogFile>().First();
            Assert.AreEqual(LogSeverity.None, file.Severity);
        }

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
