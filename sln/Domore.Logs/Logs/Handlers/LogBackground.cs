using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Domore.Logs.Handlers {
    abstract class LogBackground : LogHandler {
        static readonly BackgroundWorker Worker = new BackgroundWorker();

        protected abstract void ProtectedHandle(string entry);

        public Exception HandleThreadError { get; private set; }

        public sealed override void Handle(string entry) {
            Worker.Add(new EntryItem(this, entry));
        }

        public static void Shutdown() {
            Worker.ShutDown();
            Worker.Dispose();
        }

        class EntryItem {
            public string Entry { get; }
            public LogBackground Handler { get; }

            public EntryItem(LogBackground handler, string entry) {
                Entry = entry;
                Handler = handler;
            }
        }

        class BackgroundWorker : IDisposable {
            Thread HandleThread;
            readonly object HandleThreadLocker = new object();
            readonly BlockingCollection<EntryItem> EntryQueue = new BlockingCollection<EntryItem>();
            readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

            void HandleThreadStart() {
                for (; ; ) {
                    var item = default(EntryItem);
                    var error = default(Exception);
                    var token = CancellationTokenSource.Token;
                    try {
                        item = EntryQueue.Take(token);
                    }
                    catch (ObjectDisposedException ex) {
                        error = ex;
                    }

                    if (item == null) {
                        break;
                    }

                    if (error != null) {
                        break;
                    }

                    if (token.IsCancellationRequested) {
                        break;
                    }

                    var entry = item.Entry;
                    var handler = item.Handler;
                    try {
                        handler.ProtectedHandle(entry);
                    }
                    catch (Exception ex) {
                        handler.HandleThreadError = ex;
                    }
                }
            }

            protected virtual void Dispose(bool disposing) {
                if (disposing) {
                    EntryQueue.Dispose();
                    CancellationTokenSource.Dispose();
                }
            }

            public void Add(EntryItem item) {
                EntryQueue.Add(item);

                if (HandleThread == null) {
                    lock (HandleThreadLocker) {
                        if (HandleThread == null) {
                            HandleThread = new Thread(HandleThreadStart);
                            HandleThread.Name = typeof(LogBackground).FullName;
                            HandleThread.IsBackground = true;
                            HandleThread.Start();
                        }
                    }
                }
            }

            public void ShutDown() {
                // TODO: Fix this.
                CancellationTokenSource.Cancel();
                EntryQueue.CompleteAdding();
            }

            public void Dispose() {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            ~BackgroundWorker() {
                Dispose(false);
            }
        }
    }
}
