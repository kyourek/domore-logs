using System;
using System.IO;
using DirectoryHelper = System.IO.Directory;
using PathHelper = System.IO.Path;

namespace Domore.Logs.Handlers {
    class LogFile : LogHandler.Background {
        static bool DirectoryCreated;
        static readonly string Directory = PathHelper.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "domore",
            "log");

        static string SanitizePath(string path) {
            if (null == path) throw new ArgumentNullException(nameof(path));

            var sanePath = path.Trim();
            var invalidChars = PathHelper.GetInvalidPathChars();
            foreach (var invalidChar in invalidChars) {
                sanePath = sanePath.Replace(invalidChar, '_');
            }

            return sanePath;
        }

        DateTime? _LastWriteTime;
        DateTime? LastWriteTime {
            get {
                if (_LastWriteTime == null) {
                    var fileInfo = new FileInfo(Path);
                    if (fileInfo.Exists) {
                        _LastWriteTime = fileInfo.LastWriteTime;
                    }
                }
                return _LastWriteTime;
            }
            set => Change(ref _LastWriteTime, value, nameof(LastWriteTime));
        }

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

        string _Path;
        public string Path {
            get {
                if (_Path == null) {
                    _Path = PathHelper.Combine(Directory, Name);
                    _Path = SanitizePath(_Path);
                }
                return _Path;
            }
        }

        TimeSpan _ClearInterval = TimeSpan.FromDays(7);
        public TimeSpan ClearInterval {
            get => _ClearInterval;
            set => Change(ref _ClearInterval, value, nameof(ClearInterval));
        }
    }
}
