using System.IO;

namespace Domore.Logs.Test.Helpers {
    internal class Temp {
        public static DirectoryInfo Dir() {
            var tmp = Path.GetTempPath();
            var dir = Path.Combine(tmp, "Domore.Logs.Test");
            var inf = Directory.CreateDirectory(dir);
            return inf;
        }
    }
}
