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

    private static ModActivationPlan Plan(params LibraryMod[] mods) =>
        ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, mods, [], new Dictionary<string, string>(), [])).Value;
}
