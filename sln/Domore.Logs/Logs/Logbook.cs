using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Domore.Logs {
    using ComponentModel;

    public class Logbook : NotifyPropertyChangedImplementation, ILogHandler {
        int TruncateIndex = 0;
        readonly object Locker = new object();

        ObservableCollection<string> EntryCollection { get; }

        void EntryCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            var handler = EntriesChanged;
            if (handler != null) handler.Invoke(this, e);
        }

        public event NotifyCollectionChangedEventHandler EntriesChanged;

        public ReadOnlyObservableCollection<string> Entries { get; }

        bool _Handle = true;
        public bool Handle {
            get => _Handle;
            set => Change(ref _Handle, value, nameof(Handle));
        }

        LogSeverity _Severity = LogSeverity.None;
        public LogSeverity Severity {
            get => _Severity;
            set => Change(ref _Severity, value, nameof(Severity));
        }

        int _DataLimit;
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

        int _EntryLimit;
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

        int _TruncateDelay = 10;
        public int TruncateDelay {
            get => _TruncateDelay;
            set => Change(ref _TruncateDelay, value, nameof(TruncateDelay));
        }

        string _Data;
        public string Data {
            get => _Data ?? (_Data = "");
            private set => Change(ref _Data, value, nameof(Data));
        }

        string _Format;
        public string Format {
            get => _Format ?? (_Format = "{Data}");
            set => Change(ref _Format, value, nameof(Format));
        }

        public Logbook() {
            EntryCollection = new ObservableCollection<string>();
            EntryCollection.CollectionChanged += EntryCollection_CollectionChanged;
            Entries = new ReadOnlyObservableCollection<string>(EntryCollection);
        }

        public Logbook(params object[] logs) : this() {
            Logging.Add(this, logs);
        }

        public void Add(string entry) {
            var maxLen = DataLimit;
            var maxCnt = EntryLimit;
            if (maxLen < 1 && maxCnt < 1) return;

            lock (Locker) {
                var truncIndex = TruncateIndex + 1;
                var truncDelay = TruncateDelay;

                if (maxLen > 0) {
                    var logStr = Data + (entry ?? "").Trim() + Environment.NewLine;

                    if (truncIndex > truncDelay) {
                        var logLen = logStr.Length;
                        if (logLen > maxLen) {
                            logStr = logStr.Substring(logLen - maxLen);
                        }
                    }

                    Data = logStr;
                }

                if (maxCnt > 0) {
                    var entryColl = EntryCollection;

                    if (truncIndex > truncDelay) {
                        while (entryColl.Count > maxCnt) {
                            entryColl.RemoveAt(0);
                        }
                    }

                    entryColl.Add(entry);
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

        void ILogHandler.Handle(string entry) {
            if (Handle) {
                Add(entry);
            }
        }
    }
}
