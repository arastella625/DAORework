using System.Globalization;
using Serilog;

namespace DAORework.App.Composition;

/// <summary>Serilog is configured here only (SDD §8.2): everything to the daily log file; a window has no console.</summary>
public static class AppLogging
{
    public static Serilog.Core.Logger Create(string logsDirectory) =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(logsDirectory, "daorework-app-.log"),
                rollingInterval: RollingInterval.Day,
                formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();
}
