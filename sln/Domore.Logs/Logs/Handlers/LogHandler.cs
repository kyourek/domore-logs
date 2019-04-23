using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Domore.Logs.Handlers {
    using ComponentModel;

    abstract class LogHandler : NotifyPropertyChangedImplementation, ILogHandler {
        LogSeverity _Severity = LogSeverity.None;
        public LogSeverity Severity {
            get => _Severity;
            set => Change(ref _Severity, value, nameof(Severity));
        }

        string _Name;
        public string Name {
            get => _Name;
            set => Change(ref _Name, value, nameof(Name));
        }

        string _Format;
        public string Format {
            get => _Format;
            set => Change(ref _Format, value, nameof(Format));
        }

        public abstract void Handle(string entry);

        public abstract class Background : LogHandler {
            static Thread HandleThread;
            static readonly object EntryListLocker = new object();
            static readonly object HandleThreadLocker = new object();
            static readonly List<EntryItem> EntryList = new List<EntryItem>();
            static readonly BlockingCollection<EntryItem> EntryQueue = new BlockingCollection<EntryItem>();

            static void HandleThreadStart() {
                for (; ; ) {
                    EntryItem item;
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

            class EntryItem {
                public string Entry { get; }
                public Background Handler { get; }

                public EntryItem(Background handler, string entry) {
                    Entry = entry;
                    Handler = handler;
                }
            }
        }
    }
}
