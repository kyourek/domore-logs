# Domore.Logs.Conf

`Domore.Logs.Conf` configures `Domore.Logs` from [Domore.Conf](https://www.nuget.org/packages/Domore.Conf/) content.

Configure logging from an in-memory conf source:

```csharp
using Domore.Conf;
using Domore.Conf.Logs;

Conf.Contain(@"
    log[console].type = console
    log[console].config.default.severity = info
    log[console].config.default.format = {dat} {tim} [{sev}]
").ConfigureLogging();
```

To configure logging from a file and watch it for changes, call
`Log.Conf.Configure` once during application startup:

```csharp
using Domore.Logs;

Log.Conf.Configure("logging.conf");
```

The built-in conf container applies `log[service].type` assignments before the
other settings for each service, so service instances are available when their
settings are applied.
