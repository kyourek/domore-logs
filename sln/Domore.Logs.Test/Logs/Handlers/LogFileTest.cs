using NUnit.Framework;
using System;
using System.IO;

namespace Domore.Logs.Handlers {
    using Test.Helpers;

    [TestFixture]
    public class LogFileTest {
        LogFile Subject;
        LogQueue Queue;
        DirectoryInfo TempDir;

        [SetUp]
        public void SetUp() {
            Queue = new LogQueue();
            TempDir = Temp.Dir();
            Subject = new LogFile {
                Directory = TempDir.FullName,
                Name = Guid.NewGuid().ToString("N"),
                Queue = Queue };
        }

        [TearDown]
        public void TearDown() {
            Queue.Dispose();
            Subject.Delete();
            TempDir.Delete(true);
        }

        [Test]
        public void Handle_WritesToFile() {
            Subject.Handle("The log entry!", LogSeverity.Critical);
            Subject.Queue.Complete();
            var actual = Subject.Read();
            var expected = "The log entry!" + Environment.NewLine;
            Assert.AreEqual(expected, actual);
        }
    }
}
