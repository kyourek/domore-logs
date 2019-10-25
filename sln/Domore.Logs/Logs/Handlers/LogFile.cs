using System;
using System.IO;

namespace Domore.Logs.Handlers {
    internal class LogFile : LogQueue.Handler {
        private string ValidName {
            get {
                var name = (Name ?? "").Trim();
                if (name == "") return null;

                var invalidChars = Path.GetInvalidPathChars();
                foreach (var invalidChar in invalidChars) {
                    name = name.Replace(invalidChar, '_');
                }

                return name;
            }
        }

        FileInfo FileInfo {
            get {
                var directory = (Directory ?? "").Trim();
                if (directory == "") return null;

                var validName = (ValidName ?? "").Trim();
                if (validName == "") return null;

                var path = Path.Combine(Environment.ExpandEnvironmentVariables(directory), validName);
                var file = new FileInfo(path);

                return file;
            }
        }

        public string Directory {
            get => _Directory;
            set => Change(ref _Directory, value, nameof(Directory));
        }
        private string _Directory;

        public TimeSpan ClearInterval {
            get => _ClearInterval;
            set => Change(ref _ClearInterval, value, nameof(ClearInterval));
        }
        private TimeSpan _ClearInterval = TimeSpan.FromDays(7);

        public string Read() {
            var file = FileInfo;
            if (file == null) return null;
            if (file.Exists == false) return null;
            try {
                return File.ReadAllText(file.FullName);
            }
            catch (FileNotFoundException) {
                return null;
            }
        }

        public void Delete() {
            var file = FileInfo;
            if (file == null) return;
            if (file.Exists == false) return;
            file.Delete();
        }

        public override void HandleWork(string message, LogSeverity severity) {
            var file = FileInfo;
            if (file == null) return;

            StreamWriter open() {
                if (file.Exists) {
                    var entryTime = DateTime.UtcNow;
                    var lastWriteTime = file.LastWriteTimeUtc;
                    var eraseExisting = ClearInterval < (entryTime - lastWriteTime);
                    if (eraseExisting) {
                        return file.CreateText();
                    }
                    return file.AppendText();
                }

                var dir = file.Directory;
                if (dir.Exists == false) {
                    dir.Create();
                }
                return file.CreateText();
            }

            using (var writer = open()) {
                writer.WriteLine(message);
            }
        }
    }
}
