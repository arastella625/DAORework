using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>A mod that runs inside the game is never half-installed (P2.8-02, ADR-0033 §1).</summary>
public sealed class GameProgramPlanTests
{
    private static readonly LibraryMod _program = new("dao-lib-1", "DAO Lib", ModKind.GameProgram, [new LibraryModFile("bin_ship/dinput8.dll", 4, "a"), new LibraryModFile("readme.txt", 4, "b")]);

    [Fact]
    public void Layout_PlacesNothingInTheDocumentsFolder()
    {
        Assert.Empty(ModLayout.Map(_program).Value);
    }

    [Fact]
    public void Plan_IsBlocked_NamesTheMod_AndCopiesNothing()
    {
        var plan = Plan(_program);

        var blocker = Assert.Single(plan.Blockers);
        Assert.Equal(PlanBlockerKind.GameProgramNotInstalled, blocker.Kind);
        Assert.StartsWith("DAO Lib runs inside the game", blocker.Message, StringComparison.Ordinal);
        Assert.Empty(plan.Operations);
    }

    [Fact]
    public void Plan_WithAnotherMod_IsBlockedWhole()
    {
        Assert.True(Plan(Mods.Override("hair", "hair.dds"), _program).IsBlocked);
    }

    [Fact]
    public void Plan_WithAnotherMod_SaysToTurnItOnByItself()
    {
        var blocker = Plan(Mods.Override("hair", "hair.dds"), _program).Blockers.Single(b => b.Kind == PlanBlockerKind.GameProgramNotInstalled);

        Assert.EndsWith("Turn it on by itself: mods activate dao-lib-1 --allow-programs dao-lib-1.", blocker.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_ACopyThatIsStillOn_SaysToTurnItOffFirst()
    {
        var current = new DeploymentRecord(["dao-lib-1"], [], []);

        var plan = ModActivationPlanner.Plan(new PlanInput(current, [_program], [], new Dictionary<string, string>(), [])).Value;

        Assert.Contains("Turn DAO Lib off first", Assert.Single(plan.Blockers).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_ADxvkDownload_PointsToTheFix()
    {
        var dxvk = new LibraryMod("dxvk-1", "DXVK", ModKind.GameProgram, [new LibraryModFile("d3d9.dll", 4, "a")]);

        Assert.Contains("fixes apply dxvk", Assert.Single(Plan(dxvk).Blockers).Message, StringComparison.Ordinal);
    }

    private static ModActivationPlan Plan(params LibraryMod[] mods) =>
        ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, mods, [], new Dictionary<string, string>(), [])).Value;
}
