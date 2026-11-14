using Domore.Logs;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Windows;

/*
 * Simulates an application that logs from worker threads through ordinary
 * ILog instances. The log control receives these entries like any other
 * subscriber; nothing here refers to it.
 */
internal sealed class LogGenerator {
    private static readonly ILog[] Logs = [
        Logging.For(typeof(Network)),
        Logging.For(typeof(Database)),
        Logging.For(typeof(Scheduler))
    ];

    private long Count;
    private volatile int TargetRate;

    /*
     * Each source's type name becomes the log name shown in the viewer.
     */
    private sealed class Network {
    }

    private sealed class Database {
    }

    private sealed class Scheduler {
    }

    private static LogSeverity SeverityOf(long n) {
        return
            n % 997 == 0 ? LogSeverity.Critical :
            n % 97 == 0 ? LogSeverity.Error :
            n % 23 == 0 ? LogSeverity.Warn :
            n % 3 == 0 ? LogSeverity.Debug :
            LogSeverity.Info;
    }

    private static string MessageOf(long source, long n, LogSeverity severity) {
        var milliseconds = n * 7 % 250 + 1;
        return (source, severity) switch {
            (0, LogSeverity.Debug) => $"Connection pool: {n % 16} of 16 connections in use",
            (0, LogSeverity.Info) => $"GET /api/orders/{n} 200 in {milliseconds} ms",
            (0, LogSeverity.Warn) => $"GET /api/orders/{n} 200 in {milliseconds + 1000} ms, over the 1,000 ms budget",
            (0, _) => $"GET /api/orders/{n} failed: connection reset by peer",
            (1, LogSeverity.Debug) => $"SELECT * FROM orders WHERE id = {n} ({milliseconds} ms)",
            (1, LogSeverity.Info) => $"Saved order {n} with {n % 9 + 1} items",
            (1, LogSeverity.Warn) => $"Query for order {n} took {milliseconds + 1000} ms",
            (1, _) => $"Deadlock while saving order {n}; the transaction was rolled back",
            (_, LogSeverity.Debug) => $"Job {n} is next in 5 s",
            (_, LogSeverity.Info) => $"Job {n} finished in {milliseconds} ms",
            (_, LogSeverity.Warn) => $"Job {n} started {milliseconds} ms late",
            _ => $"Job {n} failed after 3 attempts"
        };
    }

    private static void ImportOrders() {
        throw new InvalidOperationException("The import file has no header row.");
    }

    private void Write(long n) {
        var source = n % Logs.Length;
        var log = Logs[source];
        var severity = SeverityOf(n);
        /*
         * Checking first skips formatting entries that nobody would display.
         */
        bool enabled = log.Enabled(severity);
        if (enabled) {
            log.Data(severity, MessageOf(source, n, severity));
        }
        Interlocked.Increment(ref Count);
    }

    public static string[] SourceNames { get; } = [nameof(Network), nameof(Database), nameof(Scheduler)];

    public long Logged => Interlocked.Read(ref Count);

    /*
     * Entries written per second by Run. Zero pauses it.
     */
    public int Rate {
        get => TargetRate;
        set => TargetRate = Math.Max(0, value);
    }

    /*
     * Writes entries at Rate on a worker thread until canceled.
     */
    public async Task Run(CancellationToken cancellationToken) {
        var clock = Stopwatch.StartNew();
        var rate = 0;
        var since = TimeSpan.Zero;
        var written = 0L;
        var n = 0L;
        try {
            while (true) {
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                var target = Rate;
                bool rateChanged = target != rate;
                if (rateChanged) {
                    rate = target;
                    since = clock.Elapsed;
                    written = 0;
                }
                var due = (long)(rate * (clock.Elapsed - since).TotalSeconds);
                for (; written < due; written++) {
                    Write(n++);
                }
            }
        }
        catch (OperationCanceledException) {
            /*
             * The window closed.
             */
        }
    }

    /*
     * Writes count entries as fast as possible from four threads at once.
     */
    public Task Burst(int count) {
        const int threads = 4;
        return Task.WhenAll(Enumerable.Range(0, threads).Select(thread => Task.Run(() => {
            for (long n = thread; n < count; n += threads) {
                Write(n);
            }
        })));
    }

    public static void LogException() {
        try {
            ImportOrders();
        }
        catch (Exception ex) {
            Logs[2].Error("The nightly order import failed.", ex);
        }
    }

    public static void LogUnicodeAndLongLine() {
        Logs[0].Info("Unicode: café · 日本語 · 😀 · سلام · שלום");
        Logs[0].Info("Long line: " + string.Join(" ", Enumerable.Range(0, 80).Select(i => $"field{i}=value{i}")));
    }
}
