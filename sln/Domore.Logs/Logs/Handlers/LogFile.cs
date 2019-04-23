using System;
using System.ComponentModel;
using System.IO;
using DirectoryHelper = System.IO.Directory;
using PathHelper = System.IO.Path;

namespace Domore.Logs.Handlers {
    class LogFile : LogHandler.Background {
        static string SanitizePath(string path) {
            if (null == path) throw new ArgumentNullException(nameof(path));

            var sanePath = path.Trim();
            var invalidChars = PathHelper.GetInvalidPathChars();
            foreach (var invalidChar in invalidChars) {
                sanePath = sanePath.Replace(invalidChar, '_');
            }

            return sanePath;
        }

        bool DirectoryCreated;

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

        protected override void OnPropertyChanged(PropertyChangedEventArgs e) {
            base.OnPropertyChanged(e);
            if (e != null) {
                if (e.PropertyName == nameof(Name) || e.PropertyName == nameof(Directory)) {
                    Path = null;
                }
            }
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

        string _Directory;
        public string Directory {
            get => _Directory ?? (_Directory = PathHelper.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Domore.Logs"));
            set {
                if (Change(ref _Directory, value, nameof(Directory))) {
                    DirectoryCreated = false;
                }
            }
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
            private set => Change(ref _Path, value, nameof(Path));
        }

        TimeSpan _ClearInterval = TimeSpan.FromDays(7);
        public TimeSpan ClearInterval {
            get => _ClearInterval;
            set => Change(ref _ClearInterval, value, nameof(ClearInterval));
        }
    }
}
