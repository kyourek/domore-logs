using System;
using NUnit.Framework;

namespace Domore.Logs.Handlers {
#if NETFRAMEWORK
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
    }
#endif
}
