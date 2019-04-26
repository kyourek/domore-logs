using System;
using NUnit.Framework;

namespace Domore.Logs.Handlers {
    [TestFixture]
    public class LogEventTest {
        LogEvent Subject;
        LogQueue Queue;

        [SetUp]
        public void SetUp() {
            Queue = new LogQueue();
            Subject = new LogEvent {
                Name = Guid.NewGuid().ToString("N"),
                Queue = Queue
            };
        }

        [TearDown]
        public void TearDown() {
            Queue.Dispose();
        }

        [Test]
        public void temp() {
            Subject.LogName = "Application";
            Subject.Handle("The log entry!", LogSeverity.Info);
            Subject.Queue.Complete();
            var actual = Subject.Read();
            var expected = "The log entry!" + Environment.NewLine;
            Assert.AreEqual(expected, actual);
        }
    }
}
