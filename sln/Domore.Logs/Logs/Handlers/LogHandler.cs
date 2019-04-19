using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Domore.Logs.Handlers {
    using ComponentModel;

    internal abstract class LogHandler : NotifyPropertyChangedImplementation, ILogHandler {
        public LogSeverity Severity {
            get { return _Severity; }
            set {
                if (_Severity != value) {
                    _Severity = value;
                    NotifyPropertyChanged("Severity");
                }
            }
        }
        private LogSeverity _Severity = LogSeverity.None;

        public string Name {
            get { return _Name; }
            set {
                if (_Name != value) {
                    _Name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }
        private string _Name;

        public string Format {
            get { return _Format; }
            set {
                if (_Format != value) {
                    _Format = value;
                    NotifyPropertyChanged("Format");
                }
            }
        }
        private string _Format;

        public abstract void Handle(string entry);

        internal abstract class Background : LogHandler {
            private static Thread HandleThread;
            private static readonly object EntryListLocker = new object();
            private static readonly object HandleThreadLocker = new object();
            private static readonly List<EntryItem> EntryList = new List<EntryItem>();
            private static readonly BlockingCollection<EntryItem> EntryQueue = new BlockingCollection<EntryItem>();

            private static void HandleThreadStart() {
                for (; ; ) {
                    var item = default(EntryItem);
                    try {
                        item = EntryQueue.Take();
                    }
                    catch (ObjectDisposedException) {
                        break;
                    }

                    var entry = item.Entry;
                    var handler = item.Handler;
                    try {
                        handler.HandleAction(entry);
                    }
                    catch (Exception ex) {
                        handler.HandleThreadError = ex;
                    }

                    lock (EntryListLocker) {
                        EntryList.Remove(item);
                    }
                }
            }

            protected abstract void HandleAction(string entry);

            public Exception HandleThreadError { get; private set; }

            public bool Handling {
                get {
                    lock (EntryListLocker) {
                        return EntryList
                            .Where(item => item.Handler == this)
                            .Count() > 0;
                    }
                }
            }

            public sealed override void Handle(string entry) {
                var item = new EntryItem(this, entry);
                lock (EntryListLocker) {
                    EntryList.Add(item);
                    EntryQueue.Add(item);
                }

                if (HandleThread == null) {
                    lock (HandleThreadLocker) {
                        if (HandleThread == null) {
                            HandleThread = new Thread(HandleThreadStart);
                            HandleThread.Name = typeof(Background).FullName;
                            HandleThread.IsBackground = true;
                            HandleThread.Start();
                        }
                    }
                }
            }

            private class EntryItem {
                public string Entry { get { return _Entry; } }
                private readonly string _Entry;

                public Background Handler { get { return _Handler; } }
                private readonly Background _Handler;

                public EntryItem(Background handler, string entry) {
                    _Entry = entry;
                    _Handler = handler;
                }
            }
        }
    }
}
