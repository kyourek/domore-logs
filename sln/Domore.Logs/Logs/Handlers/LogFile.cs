using System;
using System.IO;

namespace Domore.Logs.Handlers {
    class LogFile : LogBackground {
        string ValidName {
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

        protected override void ProtectedHandle(string message) {
            var directory = (Directory ?? "").Trim();
            if (directory == "") return;

            var validName = (ValidName ?? "").Trim();
            if (validName == "") return;

            var open = default(Func<StreamWriter>);
            var path = Path.Combine(directory, validName);
            var file = new FileInfo(path);
            if (file.Exists) {
                var entryTime = DateTime.Now;
                var lastWriteTime = file.LastWriteTime;
                var eraseExisting = ClearInterval < (entryTime - lastWriteTime);
                if (eraseExisting) {
                    open = file.CreateText;
                }
                else {
                    open = file.AppendText;
                }
            }
            else {
                var dir = new DirectoryInfo(directory);
                if (dir.Exists == false) {
                    dir.Create();
                }
                open = file.CreateText;
            }

            using (var writer = open()) {
                writer.WriteLine(message);
            }
        }

        string _Directory;
        public string Directory {
            get => _Directory;
            set => Change(ref _Directory, value, nameof(Directory));
        }

        TimeSpan _ClearInterval = TimeSpan.FromDays(7);
        public TimeSpan ClearInterval {
            get => _ClearInterval;
            set => Change(ref _ClearInterval, value, nameof(ClearInterval));
        }
    }
}
