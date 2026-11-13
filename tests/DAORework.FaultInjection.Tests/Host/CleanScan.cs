using DAORework.Application.Abstractions;
using DAORework.Core.Mods.Safety;

namespace DAORework.FaultInjection.Tests.Host;

/// <summary>
/// The gate's stand-in antivirus: every scan is clean. A mod that runs inside the game is only placed once a scan is
/// clean (ADR-0033 §2), and the gate is about what a kill leaves on disk, on Linux CI too, where there is no AMSI.
/// </summary>
internal sealed class CleanScan : IMalwareScanner
{
    public Task<MalwareScan> ScanAsync(IReadOnlyList<ScanTarget> files, CancellationToken cancellationToken = default) =>
        Task.FromResult(new MalwareScan(MalwareScanStatus.NoThreatFound, "The gate's stand-in antivirus."));
}
