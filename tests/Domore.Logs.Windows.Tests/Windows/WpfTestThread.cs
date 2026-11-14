using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Domore.Windows;

internal static class WpfTestThread {
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private static async Task RunOnDispatcher(Func<Task> action, Dispatcher dispatcher, Action<Exception> fail) {
        try {
            await action();
        }
        catch (Exception ex) {
            fail(ex);
        }
        finally {
            dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
        }
    }

    public static void Run(Func<Task> action) {
        Exception failure = null;
        var thread = new Thread(() => {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvoke(new Func<Task>(() => RunOnDispatcher(action, dispatcher, ex => failure = ex)));
            Dispatcher.Run();
        }) {
            IsBackground = true,
            Name = "Domore.Logs.Windows WPF test dispatcher"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        var finished = thread.Join(Timeout);
        if (finished == false) {
            throw new TimeoutException("The WPF test did not finish in time.");
        }
        var failed = failure is not null;
        if (failed) {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
