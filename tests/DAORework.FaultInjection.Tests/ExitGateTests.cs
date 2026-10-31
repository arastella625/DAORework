using DAORework.FaultInjection.Tests.Gate;
using Xunit;

namespace DAORework.FaultInjection.Tests;

/// <summary>
/// The Phase 2 exit gate (ROADMAP, ADR-0014), and the Phase 2.5 adoption scenarios (ADR-0015): every mod and fix
/// change, killed before each of its writes, recovers to exactly the before-state or the after-state and never a mix.
/// The tests share one collection, so one scenario at a time uses every processor.
/// </summary>
public sealed class ExitGateTests
{
    private static Dictionary<string, GateScenario> Gate { get; } = new[]
    {
        GateScenario.Activate,
        GateScenario.Deactivate,
        GateScenario.Switch,
        GateScenario.ApplyLargeAddressAware,
        GateScenario.RemoveLargeAddressAware,
        GateScenario.ApplyDxvk,
        GateScenario.UpgradeDxvk,
        GateScenario.RemoveDxvk,
        GateScenario.AdoptAddInInPlace,
        GateScenario.AdoptOverrideFolder,
        GateScenario.AdoptLooseFiles,
        GateScenario.AdoptDuplicateNames,
    }.ToDictionary(s => s.Name);

    public static TheoryData<string> Scenarios() => new(Gate.Keys);

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task Change_KilledBeforeAnyWrite_SettlesToExactlyOneEndState(string scenario)
    {
        var result = await GateRunner.RunAsync(Gate[scenario]);

        Assert.True(result.Failures.Count == 0, string.Join(Environment.NewLine, result.Failures));
        Assert.True(result.Kills >= 10, $"Only {result.Kills} kills: the scenario hardly writes.");
        Assert.True(result.JournalsSettled > 0, "No kill left a journal to settle, so recovery was never exercised.");
    }
}
