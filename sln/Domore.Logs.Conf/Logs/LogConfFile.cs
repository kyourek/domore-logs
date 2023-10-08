using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PATH = System.IO.Path;
using Domore.Conf;
using System;
using System.Diagnostics;
using Domore.Threading;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs {
    internal sealed class LogConfFile : IDisposable {
        private readonly object WatcherLocker = new object();
        private readonly object ConfigureLocker = new object();
        private readonly DelayedState DelayedState = new DelayedState { Delay = 1000 };
        private Action CancelDelay;
        private FileSystemWatcher Watcher;


        private void Dispose(bool disposing) {
            if (disposing) {
                using (Watcher) {
                }
            }
        }

        private void Watcher_Event(object sender, FileSystemEventArgs e) {
            if (e != null) {
                if (e.Name == Name) {
                    CancelDelay?.Invoke();
                    CancelDelay = DelayedState.Attempt(() => {
                        lock (ConfigureLocker) {
                            Configure();
                        }
                    });
                }
            }
        }

        private void Watcher_Error(object sender, ErrorEventArgs e) {
            using (Watcher) {
                if (e != null) {
                    try { Console.WriteLine(e?.GetException()); } catch { }
                    try { Trace.WriteLine(e?.GetException()); } catch { }
                }
            }
        }

        private void Watcher_Disposed(object sender, EventArgs e) {
        }

        private void Watch() {
            if (Watcher == null) {
                lock (WatcherLocker) {
                    if (Watcher == null) {
                        Watcher = new FileSystemWatcher();
                        Watcher.Changed += Watcher_Event;
                        Watcher.Created += Watcher_Event;
                        Watcher.Deleted += Watcher_Event;
                        Watcher.Renamed += Watcher_Event;
                        Watcher.Error += Watcher_Error; ;
                        Watcher.Disposed += Watcher_Disposed; ;
                        Watcher.Path = Directory;
                        Watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastWrite;
                        Watcher.IncludeSubdirectories = false;
                        Watcher.InternalBufferSize = 65536;
                        Watcher.EnableRaisingEvents = true;
                    }
                }
            }
        }

        private string Read() {
            try {
                return File.ReadAllText(Path);
            }
            catch (FileNotFoundException) {
                return "";
            }
        }

        private object Configure() {
            var text = Read();
            var conf = CONF.Contain(text);
            return conf.Configure(Logging.Config, key: "");
        }

        public string Path { get; }
        public string Name { get; }
        public string Directory { get; }

        public LogConfFile(string path) {
            Path = path;
            Name = PATH.GetFileName(Path);
            Directory = PATH.GetDirectoryName(Path);


            Configure(default).ContinueWith(_ => {
                Event.Handler += Event_Handler;
            });
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~LogConfFile() {
            Dispose(false);
        }
    }
}
