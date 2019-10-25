using NUnit.Framework;
using System;

namespace Domore.Logs.Handlers {
#if NETFRAMEWORK
    [TestFixture]
    public class LogEventTest {
        private LogEvent Subject;
        private LogQueue Queue;

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
    }
#endif
}
