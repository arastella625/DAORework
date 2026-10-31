using System.Diagnostics;
using System.Globalization;
using DAORework.FaultInjection.Tests.Host;

namespace DAORework.FaultInjection.Tests.Gate;

/// <summary>Starts this assembly as the CLI in its own process, to be killed before write number killAt (ADR-0014).</summary>
internal static class ChildCli
{
    public const int NeverKill = -1;

    public static async Task<KilledRun> RunAsync(string appData, int killAt, IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in new[] { typeof(Program).Assembly.Location, FaultHost.Verb, killAt.ToString(CultureInfo.InvariantCulture), appData }.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("The CLI process did not start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await process.WaitForExitAsync(timeout.Token);
        var error = await errors;
        var killLine = error.Split('\n').FirstOrDefault(l => l.StartsWith(KillingFileSystem.KillPrefix, StringComparison.Ordinal));
        var writesLine = error.Split('\n').FirstOrDefault(l => l.StartsWith(FaultHost.WritesPrefix, StringComparison.Ordinal));
        var writes = writesLine is null ? -1 : int.Parse(writesLine[FaultHost.WritesPrefix.Length..].Trim(), CultureInfo.InvariantCulture);
        return new KilledRun(process.ExitCode, killLine is not null, killLine?.Trim() ?? string.Empty, writes, await output + error);
    }
}
