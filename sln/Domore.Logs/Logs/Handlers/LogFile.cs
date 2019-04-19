using System;
using System.IO;
using DirectoryHelper = System.IO.Directory;
using PathHelper = System.IO.Path;

namespace Domore.Logs.Handlers {
    internal class LogFile : LogHandler.Background {
        private static bool DirectoryCreated;
        private static readonly string Directory = PathHelper.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "domore",
            "log");

        private static string SanitizePath(string path) {
            if (null == path) throw new ArgumentNullException("path");

            var sanePath = path.Trim();
            var invalidChars = PathHelper.GetInvalidPathChars();
            foreach (var invalidChar in invalidChars) {
                sanePath = sanePath.Replace(invalidChar, '_');
            }

            return sanePath;
        }

        private DateTime? LastWriteTime {
            get {
                if (_LastWriteTime == null) {
                    var fileInfo = new FileInfo(Path);
                    if (fileInfo.Exists) {
                        _LastWriteTime = fileInfo.LastWriteTime;
                    }
                }
                return _LastWriteTime;
            }
            set {
                _LastWriteTime = value;
            }
        }
        private DateTime? _LastWriteTime;

        protected override void HandleAction(string message) {
            if (DirectoryCreated == false) {
                if (DirectoryHelper.Exists(Directory) == false) {
                    DirectoryHelper.CreateDirectory(Directory);
                }
                DirectoryCreated = true;
            }

            var path = Path;
            var entryTime = DateTime.Now;
            var lastWriteTime = LastWriteTime;
            if (lastWriteTime.HasValue) {
                if (ClearInterval < (entryTime - lastWriteTime.Value)) {
                    File.WriteAllText(path, "");
                }
            }

            File.AppendAllText(path, message + Environment.NewLine);
            LastWriteTime = entryTime;
        }

        public string Path {
            get {
                if (_Path == null) {
                    _Path = PathHelper.Combine(Directory, Name);
                    _Path = SanitizePath(_Path);
                }
                return _Path;
            }
        }
        private string _Path;

        public TimeSpan ClearInterval {
            get { return _ClearInterval; }
            set { _ClearInterval = value; }
        }
        private TimeSpan _ClearInterval = TimeSpan.FromDays(7);
    }
}
