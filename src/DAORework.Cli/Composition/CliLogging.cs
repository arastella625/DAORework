using System.Globalization;
using Serilog;
using Serilog.Events;

namespace DAORework.Cli.Composition;

/// <summary>Serilog is configured here only (SDD §8.2): warnings and above on the console, everything to the daily log file.</summary>
public static class CliLogging
{
    public static Serilog.Core.Logger Create(string logsDirectory) =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Warning, formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                Path.Combine(logsDirectory, "daorework-.log"),
                rollingInterval: RollingInterval.Day,
                formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();
}
