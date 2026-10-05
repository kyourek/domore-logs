# Domore.Logs

`Domore.Logs` is a lightweight, simple, and opinionated .NET logging library.
It supports console, debug, trace, and file targets, and ships with SourceLink
and symbol packages for source-level debugging.

Install it with:

```bash
dotnet add package Domore.Logs
```

## Usage

A lightweight, simple, and very opinionated logging library. Create a log per type, and write to the console, debug output, trace, or files. Logging runs on a background queue, so it stays out of your hot paths.

```csharp
using Domore.Logs;

class Sample {
    private static readonly ILog Log = Logging.For(typeof(Sample));

    static void Main() {
        if (Log.Debug()) Log.Debug($"Now it's {DateTime.Now}."); // cheap check before formatting
        Log.Info("This is the logging sample.");
        Log.Warn("Hey! Look out!");

        Logging.Complete(); // flush pending entries before exit
    }
}
```

Severity thresholds, per-log formats, console colors, custom formatters for your own types, and custom `ILogService` handlers are all configurable at runtime.

## License

[MIT](LICENSE) © Ken Yourek
