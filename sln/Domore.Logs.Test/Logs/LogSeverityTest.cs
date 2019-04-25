using NUnit.Framework;

namespace Domore.Logs {
    [TestFixture]
    public class LogSeverityTest {
        [Test]
        public void Info_IsGreaterThanDebug() {
            Assert.IsTrue(LogSeverity.Info > LogSeverity.Debug);
        }

        [Test]
        public void Warn_IsGreaterThanInfo() {
            Assert.IsTrue(LogSeverity.Warn > LogSeverity.Info);
        }

        [Test]
        public void Error_IsGreaterThanWarn() {
            Assert.IsTrue(LogSeverity.Error > LogSeverity.Warn);
        }

        [Test]
        public void Critical_IsGreaterThanError() {
            Assert.IsTrue(LogSeverity.Critical > LogSeverity.Error);
        }
    }
}
