using Domore.Logs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

namespace Domore.Windows.Controls;

/*
 * Receives entries on logging threads and hands them to the dispatcher in
 * batches. At most one flush is queued at a time, and pending entries beyond
 * the limit are discarded oldest first because they could not be displayed.
 */
internal sealed class LogSubscription : ILogSubscription {
    private readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        Locker = new();

    private readonly Dispatcher Dispatcher;
    private readonly Action<IReadOnlyCollection<ILogEntry>> Deliver;
    private Queue<ILogEntry> Pending = new();
    private bool FlushQueued;

    private void Flush() {
        Queue<ILogEntry> batch;
        lock (Locker) {
            batch = Pending;
            Pending = new Queue<ILogEntry>();
            FlushQueued = false;
        }
        Deliver(batch);
    }

    private void QueueFlush() {
        bool shutdownStarted = Dispatcher.HasShutdownStarted;
        if (shutdownStarted) {
            return;
        }
        /*
         * Logging marks callbacks through the execution context. The flush is
         * not part of the callback, so the context does not flow into it.
         */
        bool flowIsSuppressed = ExecutionContext.IsFlowSuppressed();
        if (flowIsSuppressed) {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Flush));
        }
        else {
            using (ExecutionContext.SuppressFlow()) {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Flush));
            }
        }
    }

    public event EventHandler ThresholdChanged;

    public LogFilter Filter {
        get {
            lock (Locker) {
                return field;
            }
        }
        set {
            bool valueIsNull = value is null;
            if (valueIsNull) {
                throw new ArgumentNullException(nameof(Filter));
            }
            lock (Locker) {
                field = value;
            }
            ThresholdChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int PendingLimit {
        get {
            lock (Locker) {
                return field;
            }
        }
        set {
            bool valueIsInvalid = value < 1;
            if (valueIsInvalid) {
                throw new ArgumentOutOfRangeException(nameof(PendingLimit), value, "The value must be positive.");
            }
            lock (Locker) {
                field = value;
                bool overLimit = Pending.Count > field;
                while (overLimit) {
                    Pending.Dequeue();
                    overLimit = Pending.Count > field;
                }
            }
        }
    }

    public LogSubscription(Dispatcher dispatcher, Action<IReadOnlyCollection<ILogEntry>> deliver, LogFilter filter, int pendingLimit) {
        Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        Deliver = deliver ?? throw new ArgumentNullException(nameof(deliver));
        Filter = filter;
        PendingLimit = pendingLimit;
    }

    public LogSeverity Threshold(Type type) {
        return Filter.ThresholdFor(type);
    }

    public void Receive(ILogEntry entry) {
        bool entryIsNull = entry is null;
        if (entryIsNull) {
            return;
        }
        bool queueFlush;
        lock (Locker) {
            var limit = PendingLimit;
            Pending.Enqueue(entry);
            bool overLimit = Pending.Count > limit;
            while (overLimit) {
                Pending.Dequeue();
                overLimit = Pending.Count > limit;
            }
            queueFlush = FlushQueued == false;
            FlushQueued = true;
        }
        if (queueFlush) {
            QueueFlush();
        }
    }
}
