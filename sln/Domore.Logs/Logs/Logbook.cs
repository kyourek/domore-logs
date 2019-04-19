using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Domore.Logs {
    using ComponentModel;

    public class Logbook : NotifyPropertyChangedImplementation, ILogHandler {
        private int TruncateIndex = 0;
        private readonly object Locker = new object();

        private ObservableCollection<string> EntryCollection { get { return _EntryCollection; } }
        private readonly ObservableCollection<string> _EntryCollection;

        private void EntryCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            var handler = EntriesChanged;
            if (handler != null) handler.Invoke(this, e);
        }

        public event NotifyCollectionChangedEventHandler EntriesChanged;

        public ReadOnlyObservableCollection<string> Entries { get { return _Entries; } }
        private readonly ReadOnlyObservableCollection<string> _Entries;

        public bool Handle {
            get { return _Handle; }
            set {
                if (_Handle != value) {
                    _Handle = value;
                    NotifyPropertyChanged("Handle");
                }
            }
        }
        private bool _Handle = true;

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

        public int DataLimit {
            get { return _DataLimit; }
            set {
                if (_DataLimit != value) {
                    _DataLimit = value;
                    NotifyPropertyChanged("DataLimit");

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
            get { return _EntryLimit; }
            set {
                if (_EntryLimit != value) {
                    _EntryLimit = value;
                    NotifyPropertyChanged("EntryLimit");

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
            get { return _TruncateDelay; }
            set {
                if (_TruncateDelay != value) {
                    _TruncateDelay = value;
                    NotifyPropertyChanged("TruncateDelay");
                }
            }
        }
        private int _TruncateDelay = 10;

        public string Data {
            get { return _Data ?? (_Data = ""); }
            private set {
                if (_Data != value) {
                    _Data = value;
                    NotifyPropertyChanged("Data");
                }
            }
        }
        private string _Data;

        public string Format {
            get { return _Format ?? (_Format = "{Data}"); }
            set {
                if (_Format != value) {
                    _Format = value;
                    NotifyPropertyChanged("Format");
                }
            }
        }
        private string _Format;

        public Logbook() {
            _EntryCollection = new ObservableCollection<string>();
            _EntryCollection.CollectionChanged += EntryCollection_CollectionChanged;
            
            _Entries = new ReadOnlyObservableCollection<string>(_EntryCollection);
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
            var limit = default(int);

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
