using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Domore.Logs {
    public class Logbook : LogHandler {
        private int TruncateIndex = 0;
        private readonly object Locker = new object();
        private readonly ObservableCollection<LogbookEntry> EntryCollection;

        private void EntryCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            EntriesChanged?.Invoke(this, e);

        public event NotifyCollectionChangedEventHandler EntriesChanged;

        public ReadOnlyObservableCollection<LogbookEntry> Entries { get; }

        public bool Handling {
            get => _Handling;
            set => Change(ref _Handling, value, nameof(Handling));
        }
        private bool _Handling = true;

        public int DataLimit {
            get => _DataLimit;
            set {
                if (Change(ref _DataLimit, value, nameof(DataLimit))) {
                    lock (Locker) {
                        if (Data.Length > value) {
                            Data = Data.Substring(Data.Length - value);
                        }
                    }
                }
            }
        }
        private int _DataLimit;

        public int EntryLimit {
            get => _EntryLimit;
            set {
                if (Change(ref _EntryLimit, value, nameof(EntryLimit))) {
                    lock (Locker) {
                        while (EntryCollection.Count > value) {
                            EntryCollection.RemoveAt(0);
                        }
                    }
                }
            }
        }
        private int _EntryLimit;

        public int TruncateDelay {
            get => _TruncateDelay;
            set => Change(ref _TruncateDelay, value, nameof(TruncateDelay));
        }
        private int _TruncateDelay = 10;

        public string Data {
            get => _Data ?? (_Data = "");
            private set => Change(ref _Data, value, nameof(Data));
        }
        private string _Data;

        public Logbook() {
            EntryCollection = new ObservableCollection<LogbookEntry>();
            EntryCollection.CollectionChanged += EntryCollection_CollectionChanged;
            Entries = new ReadOnlyObservableCollection<LogbookEntry>(EntryCollection);
        }

        public Logbook(params object[] logs) : this() {
            Logging.Add(this, logs);
        }

        public void Add(string message, LogSeverity severity) {
            var maxLen = DataLimit;
            var maxCnt = EntryLimit;
            if (maxLen < 1 && maxCnt < 1) return;

            lock (Locker) {
                var truncIndex = TruncateIndex + 1;
                var truncDelay = TruncateDelay;

                if (maxLen > 0) {
                    var logStr = Data + (message ?? "").Trim() + Environment.NewLine;

                    if (truncIndex > truncDelay) {
                        var logLen = logStr.Length;
                        if (logLen > maxLen) {
                            logStr = logStr.Substring(logLen - maxLen);
                        }
                    }

                    Data = logStr;
                }

                if (maxCnt > 0) {
                    if (truncIndex > truncDelay) {
                        while (EntryCollection.Count > maxCnt) {
                            EntryCollection.RemoveAt(0);
                        }
                    }
                    EntryCollection.Add(new LogbookEntry(message, severity));
                }

                TruncateIndex = truncIndex > truncDelay
                    ? 0
                    : truncIndex;
            }
        }

        public void Clear() {
            int limit;

            limit = DataLimit;
            DataLimit = 0;
            DataLimit = limit;

            limit = EntryLimit;
            EntryLimit = 0;
            EntryLimit = limit;
        }

        public override void Handle(string message, LogSeverity severity) {
            if (Handling) {
                Add(message, severity);
            }
        }
    }
}
